using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq;
using MyMonitorHub.Common.WebApi;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Exceptions;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Models;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Util;


namespace MyMonitorHub.Domain.Service
{
    public class ItemService
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;
        public ItemService(IDbContextScopeFactory contextScopeFactory)
        {
            _contextScopeFactory = contextScopeFactory;
        }

        public Item Get(int accountId, int deviceId, string category, string subCategory, string itemName,
            ItemStyle style)
        {
            if (deviceId == 0) return null;

            using (var scope = _contextScopeFactory.Create())
            {
                var catN =
                    scope.Get<Category>().FirstOrDefault(x => x.AccountId == accountId && x.Description == category);
                if (catN == null) return null;

                var item =
                    scope.Get<Item>().FirstOrDefault(x => x.CategoryId == catN.CategoryId && x.SubCategoryName == subCategory && x.Description == itemName &&
                                                          x.DeviceId == deviceId);
                return item;
            }
        }

        public Item GetOrCreate(int accountId, Device device, string category, string subCategory, string itemName,
            ItemStyle style)
        {
            var cat = new CategoryService(_contextScopeFactory).GetOrCreateCatName(accountId, category);

            using (var scope = _contextScopeFactory.Create())
            {
                Item item = null;
                if (cat.CategoryId != 0 && device.DeviceId != 0)
                {
                    item =
                        scope.Get<Item>().FirstOrDefault(x => x.CategoryId == cat.CategoryId && x.SubCategoryName == subCategory && x.Description == itemName &&
                                                              x.DeviceId == device.DeviceId);
                }

                if (item != null)
                {
                    return item;
                }

                if (device.DeviceId == 0) scope.Attach(device);

                item = new Item
                {
                    Description = itemName,
                    Device = device,
                    Status = (int)EventType.Unknown,
                    StatusDescription = "",
                    SubCategoryName = subCategory,
                    AccountId = accountId,
                    Category = cat,
                    TimeStamp = DateTime.Now,
                    ConstantlyReportsInYN = DoesSendHealth(style)
                };
                
                scope.Add(item);
                scope.SaveChanges();
                return item;
            } // using
        }

        public static bool DoesSendHealth(ItemStyle style)
        {
            if (style == ItemStyle.SendsHealth)
            {
                return true;
            }
            return false;
        }

        public ItemLayoutView GetItemLayout(int pageId, int deviceGroupId, int deviceId, bool readOnly, string pathBase = "")
        {
            var deviceConnections = AgentConnections.Current.GetStatus();
            var page = new ItemLayoutView();

            using (var scope = _contextScopeFactory.Create())
            {
                if (!Authorizer.IsAdministrator)
                {
                    var accountId =
                        scope.Get<Page>().Where(x => x.PageId == pageId).Select(x => x.AccountId).FirstOrDefault();
                    if (accountId != Helper.AccountId)
                    {
                        throw new SecurityException("You only have permission to access your own account pages.");
                    }
                }


                // get all the item columns for the page
                var categoryNames = (from i in scope.Get<Item>()
                    join c in scope.Get<Category>() on i.CategoryId equals c.CategoryId
                    join d in scope.Get<Device>() on i.DeviceId equals d.DeviceId
                    join dg in scope.Get<DeviceGroup>() on d.DeviceGroupId equals dg.DeviceGroupId
                    where dg.PageId == pageId
                    select c).Distinct().ToArray();


                // set up the page headers
                foreach (var nameRow in categoryNames)
                {
                    page.Headers.Add(nameRow.Description);
                }

                IList<DeviceGroup> deviceGroups;
                if (deviceId == -1)
                {
                    if (deviceGroupId == -1)
                    {
                        // all groups for the page
                        deviceGroups = scope.Get<DeviceGroup>().Where(x => x.PageId == pageId).Include(x => x.Devices).ToList();
                    }
                    else
                    {
                        // we are looking for a specific group
                        deviceGroups =
                            scope.Get<DeviceGroup>().Where(x => x.DeviceGroupId == deviceGroupId).Include(x => x.Devices).ToList();
                    }
                }
                else
                {
                    // we are looking for one device
                    deviceGroups =
                        scope.Get<Device>().Where(x => x.DeviceId == deviceId).Select(x => x.DeviceGroup).ToList();
                }

                deviceGroups = deviceGroups.OrderBy(x => x.Description).ToList();

                foreach (var dg in deviceGroups)
                {
                    var deviceGroup = new ItemLayoutViewDeviceGroup();
                    page.DeviceGroups.Add(deviceGroup);
                    deviceGroup.Description = dg.Description;
                    deviceGroup.DeviceGroupId = dg.DeviceGroupId;

                    foreach (var deviceRow in dg.Devices)
                    {
                        if (deviceId != -1)
                        {
                            if (deviceRow.DeviceId != deviceId) continue;
                        }
                        if (deviceRow.Deleted) continue;

                        var device = new ItemLayoutViewDevice();
                        deviceGroup.Devices.Add(device);
                        device.Description = deviceRow.Description;
                        device.DeviceId = deviceRow.DeviceId;


                        var deviceConnection = (from dc in deviceConnections
                            where dc.DeviceId == device.DeviceId
                            select dc).FirstOrDefault();
                        if (deviceConnection == null)
                        {
                            device.Status = ItemLayoutViewStatus.Disconnected;
                        }
                        else
                        {
                            device.Status = ItemLayoutViewStatus.Connected;
                        }


                        // for each column on the page 
                        foreach (var nameRow in categoryNames)
                        {
                            // change db.items to namerow.items because items already cached in namerow
                            var items = (from i in nameRow.Items
                                where i.DeviceId == deviceRow.DeviceId
                                select i).ToList();

                            if (items.Count != 0)
                            {
                                // we are being monitored

                                var earliest = DateTime.Now;
                                var statusCode = 0;
                                foreach (var item in items)
                                {
                                    statusCode = (item.Status > statusCode ? item.Status : statusCode);
                                    if (item.ConstantlyReportsInYN)
                                    {
                                        earliest = (DateTime.Compare(item.TimeStamp, earliest) < 0
                                            ? item.TimeStamp
                                            : earliest);
                                    }
                                }
                                var code = statusCode.ToString(CultureInfo.InvariantCulture);
                                switch (statusCode)
                                {
                                    case (int)EventType.Ok:
                                        code = "ms-icon16-ok";
                                        break;
                                    case (int)EventType.Fail:
                                        code = "ms-icon16-fail";
                                        break;
                                    case 4:
                                        break;
                                } // switch

                                var timespan = DateTime.Now - earliest;
                                if (timespan.TotalMinutes > 15)
                                {
                                    code = "ms-icon16-really-late";
                                }

                                var span = string.Format("<span class='ms-icon16 {0}'/>", code);
                                if (readOnly)
                                {
                                    device.Codes.Add(span);
                                }
                                else
                                {
                                    var myUrl = pathBase + "/Device/Detail/" + deviceRow.DeviceId + "?filter=" + Uri.EscapeDataString(nameRow.Description);
                                    var myUrl2 = pathBase + "/Device/DetailOnly/" + deviceRow.DeviceId + "?filter=" + Uri.EscapeDataString(nameRow.Description);

                                    var link = string.Format("<a class='cluetip' href='{0}' rel='{2}'>{1}</a>", myUrl,
                                        span, myUrl2);

                                    device.Codes.Add(link);
                                }
                            }
                            else
                            {
                                var span = string.Format("<span class='ms-icon16 {0}'/>", "ms-icon16-dot");
                                device.Codes.Add(span);
                            } // end if
                        } // columns
                    } // devices
                } // deviceGroups
                return page;
            } // using
        }
 
        public ResponseGraphService.SecurityInfo GetSecurityInfo(int itemId)
        {
            using (var scope = _contextScopeFactory.Create())
            {
                return scope.Get<Item>().Where(x => x.ItemId == itemId).Select(x => new ResponseGraphService.SecurityInfo
                {
                    accountId = x.AccountId,
                    deviceGroupId = x.Device.DeviceGroup.DeviceGroupId,
                    pageId = x.Device.DeviceGroup.Page.PageId
                }).FirstOrDefault();
            }
        }

        public int GetWebServerId(int deviceGroupId)
        {
            using (var scope = _contextScopeFactory.Create())
            {
                return scope.Get<Item>().Where(x => x.Device.DeviceGroupId == deviceGroupId && x.Category.Description == "HttpVortex" &&
                                                    x.SubCategoryName == "ADS").Select(x => x.ItemId).FirstOrDefault();
            }
        }
    } // class
}