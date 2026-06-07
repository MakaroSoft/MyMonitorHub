using System;
using System.Collections.Generic;
using System.Linq;
using MyMonitorHub.Domain.Util;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Exceptions;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Models;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Device = MyMonitorHub.Domain.Entity.Device;
using DeviceGroup = MyMonitorHub.Domain.Entity.DeviceGroup;

namespace MyMonitorHub.Domain.Service
{
    /// <summary>
    ///     Summary description for Pages
    /// </summary>
    public class DeviceService
    {
        // ReSharper disable once UnusedMember.Local
        private readonly ILogger _logger;

        private readonly IDbContextScopeFactory _dbContextScopeFactory;
        public DeviceService(IDbContextScopeFactory dbContextScopeFactory, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DeviceService>();
            _dbContextScopeFactory = dbContextScopeFactory;
        }




        public Device GetOrCreate(String deviceName, DeviceGroup deviceGroup, int accountId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                Device deviceRow = null;
                if (deviceGroup.DeviceGroupId != 0)
                {
                    deviceRow =
                        scope.Get<Device>().FirstOrDefault(x => x.Description == deviceName && x.DeviceGroupId == deviceGroup.DeviceGroupId && x.AccountId == accountId);
                }

                if (deviceRow == null)
                {
                    // create the page
                    scope.Attach(deviceGroup); // may have been from a differt db context in which case it will be detached

                    deviceRow = new Device
                    {
                        Description = deviceName,
                        AccountId = accountId,
                        DeviceGroup = deviceGroup,
                        ChildIndex = 0
                    };

                    // will be fine. pagemaint will recalcute it.

                    scope.Add(deviceRow);
                    scope.SaveChanges();
                }
                return deviceRow;
            } // using
        }

        public Device Get(int deviceId, int accountId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                return scope.Get<Device>()
                            .FirstOrDefault(
                                x => x.DeviceId == deviceId && x.AccountId == accountId && x.Deleted == false);
            } // using
        }


        public DeviceLayoutModel GetDeviceLayoutModel(int deviceId)
        {
            return GetDeviceLayoutModel(deviceId, null);
        }

        public class DeviceInfo
        {
            public int DeviceId { get; set; }
            public string Description { get; set; }
            public string Disabled { get; set; }
            public string Url { get; set; }
        }

        public List<DeviceInfo> GetAllDevicesInSameGroup(int deviceId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                // big assumption that device is attached to a device group??

                var deviceGroupId = (from x in scope.Get<Device>()
                                     where x.DeviceId == deviceId
                                     select x.DeviceGroupId).First();

                var devicesInGroup = (from x in scope.Get<Device>()
                    where x.DeviceGroupId == deviceGroupId
                    select new DeviceInfo
                    {
                        DeviceId = x.DeviceId,
                        Description = x.Description
                    }).ToList();

                var deviceConnections = AgentConnections.Current.GetStatus();

                foreach (var device in devicesInGroup)
                {
                    var deviceConnection = (from dc in deviceConnections
                                            where dc.DeviceId == device.DeviceId
                                            select dc).FirstOrDefault();
                    device.Disabled = "disabled";
                    if (deviceConnection != null)
                    {
                        device.Disabled = deviceConnection.Status == ConnectionStatus.Connected ? "" : "disabled";
                    }
                }
                return devicesInGroup;
            }
        }

        public DeviceLayoutModel GetDeviceLayoutModel(int deviceId, string filter, string pathBase = "")
        {
                        var model = new DeviceLayoutModel();
            using (var scope = _dbContextScopeFactory.Create())
            {
                if (!Authorizer.IsAdministrator)
                {
                    var securityInfo = scope.Get<Device>().Where(x => x.DeviceId == deviceId).Select(x => new
                    {
                        accountId = x.AccountId,
                        deviceGroupId = x.DeviceGroupId,
                        pageId = x.DeviceGroup == null ? 0 : x.DeviceGroup.PageId
                    }).FirstOrDefault();

                    if (securityInfo == null || securityInfo.accountId != Helper.AccountId)
                    {
                        throw new SecurityException("You only have permission to access your own account pages.");
                    }
                    // filter based on permissions
                    if (securityInfo.deviceGroupId == null)
                    {
                        throw new SecurityException("Access denied to Device.");
                    }

                }


                var data = string.IsNullOrEmpty(filter)
                    ? scope.Get<Item>().Where(x => x.DeviceId == deviceId)
                    : scope.Get<Item>().Where(x => x.DeviceId == deviceId && x.Category.Description == filter);

                var myData = (from i in data
                    group i by new {i.CategoryId, Description = i.Category.Description + "> " + i.SubCategoryName}
                    into g
                    select new DeviceLayoutModelItemGroup
                    {
                        Description = g.Key.Description,
                        Items = g.Select(a => new DeviceLayoutModelItem
                        {
                            Timestamp = a.TimeStamp,
                            Description = a.Description,
                            Status = a.Status,
                            ConstantlyReportsIn = a.ConstantlyReportsInYN,
                            StatusDescription = a.StatusDescription
                        })
                    }).ToList();

                var info = (from dev in scope.Get<Device>().Where(x => x.DeviceId == deviceId)
                    select new
                    {
                        pageId = dev.DeviceGroup.PageId,
                        dname = dev.Description,
                        gname = dev.DeviceGroup.Description,
                        deviceGroupId = dev.DeviceGroupId,
                        acctId = dev.AccountId,
                        webUrl = dev.WebUrl,
                        notes = dev.Notes,
                        deviceGroupNotes = dev.DeviceGroup.Notes
                    }).First();

                model.WebUrl = info.webUrl;
                if (!string.IsNullOrEmpty(info.webUrl))
                {
                    var index = info.webUrl.IndexOf("/",10, StringComparison.Ordinal);
                    index = info.webUrl.IndexOf("/", index + 1, StringComparison.Ordinal); // find the next one

                    model.WebPingUrl = info.webUrl.Substring(0,index) + "/servlet/Broker?env=ads&template=ads.Ping/Ping.xml";
                }

                model.DeviceGroupId = info.deviceGroupId;
                model.DeviceGroupName = info.gname + " > " + info.dname;

                // collect all the other devices for this customer
                var otherDevices = from x in scope.Get<Device>()
                    where x.DeviceGroupId == info.deviceGroupId && x.DeviceId != deviceId
                    select new {x.DeviceId, x.Description};

                foreach (var otherDevice in otherDevices)
                {
                    model.OtherDevices.Add(new KeyValuePair<int, string>(otherDevice.DeviceId, otherDevice.Description));
                }

                model.Notes = Markdig.Markdown.ToHtml(info.notes ?? string.Empty);
                model.DeviceGroupNotes = Markdig.Markdown.ToHtml(info.deviceGroupNotes ?? string.Empty);

                model.DeviceName = info.dname;

                if (string.IsNullOrEmpty(filter))
                {
// ReSharper disable once Mvc.ActionNotResolved
// ReSharper disable once Mvc.ControllerNotResolved
                    var myUrl = pathBase + "/Page/Detail/" + info.pageId + "#" + info.deviceGroupId;

                    model.Description = $"<a class='underline' href='{myUrl}'>{info.gname}</a> > {info.dname}";
                    model.GroupDescriptionHtml = $"<a class='underline' href='{myUrl}'>{info.gname}</a>";
                    model.CleanDescription = $"{info.gname} {info.dname}";
                    model.ItemGroups = myData;
                }
                else
                {
                    // ReSharper disable once Mvc.ActionNotResolved
                    // ReSharper disable once Mvc.ControllerNotResolved
                    var groupNameUrl = pathBase + "/Page/Detail/" + info.pageId + "#" +
                                       info.deviceGroupId;
                    // ReSharper disable once Mvc.ActionNotResolved
                    // ReSharper disable once Mvc.ControllerNotResolved
                    var deviceNameUrl = pathBase + "/Device/Detail/" + deviceId;

                    model.Description =
                        $"<a class='underline' href='{groupNameUrl}'>{info.gname}</a> > <a class='underline' href='{deviceNameUrl}'>{info.dname}</a> > {filter}";
                    model.GroupDescriptionHtml = $"<a class='underline' href='{groupNameUrl}'>{info.gname}</a>";

                    model.ItemGroups = myData;
                }


                var deviceConnections = AgentConnections.Current.GetStatus();

                ConnectionStatus? cs = null;
                var deviceConnection = (from dc in deviceConnections
                    where dc.DeviceId == deviceId
                    select dc).FirstOrDefault();
                if (deviceConnection != null)
                {
                    cs = deviceConnection.Status;
                }
                model.ConnectionStatus = cs;


            }
            model.DeviceId = deviceId;

            // modify the header to add in a hyperlink
            if (string.IsNullOrEmpty(filter))
            {
                foreach (var itemGroup in model.ItemGroups)
                {
                    var index = itemGroup.Description.IndexOf(">", StringComparison.Ordinal);
                    if (index == -1)
                    {
                        itemGroup.Description =
                            $"<a style='text-decoration: underline;' href='{pathBase}/Device/Detail/{deviceId}?filter={Uri.EscapeDataString(itemGroup.Description)}'>{itemGroup.Description}</a>";
                    }
                    else
                    {
                        var first = itemGroup.Description.Substring(0, index).Trim();
                        var rest = itemGroup.Description.Substring(index + 1).Trim();
                        itemGroup.Description =
                            $"<a style='text-decoration: underline;' href='{pathBase}/Device/Detail/{deviceId}?filter={Uri.EscapeDataString(first)}'>{first}</a> > {rest}";
                    }
                }
            }
            return model;
        }

        public List<Device> GetDevicesForGroup(int deviceGroupId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                return scope.Get<Device>().Where(x => x.DeviceGroupId == deviceGroupId).ToList();
            }
        }

        public int[] GetDevices(int deviceGroupId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                return
                    (from d in scope.Get<Device>().Where(x => x.DeviceGroup.DeviceGroupId == deviceGroupId && x.Deleted == false)
                     select d.DeviceId).ToArray();
            }
        }
        public List<TextValuePair> GetKeyValuePairs()
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                if (!Helper.IsAdministrator)
                {
                    return scope.Get<Device>().Where(x => x.AccountId == Helper.AccountId)
                        .Select(x => new TextValuePair {Text = x.Description, Value = x.Description})
                        .Distinct()
                        .ToList();
                }
                return scope.Get<Device>()
                    .Select(x => new TextValuePair {Text = x.Description, Value = x.Description})
                    .Distinct()
                    .ToList();
            } // using
        }

        public string DeviceAndGroupName(int deviceId, IUrlHelper url)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var result = (scope.Get<Device>().Where(x => x.DeviceId == deviceId)
                    .Select(x => new
                    {
                        GroupDescription = x.DeviceGroup.Description,
                        GroupId = x.DeviceGroup.DeviceGroupId,
                        x.DeviceGroup.PageId,
                        DeviceDescription = x.Description,
                        x.DeviceId
                    }))
                    .First();
                var myUrl = url.Action("Detail", "Page", new { id = result.PageId }) + "#" + result.GroupId;
                var myUrl2 = url.Action("Detail", "Device", new { id = result.DeviceId });
                return
                    $"<a class='underline' href='{myUrl}'>{result.GroupDescription}</a> > <a class='underline' href='{myUrl2}'>{result.DeviceDescription}</a>";
            }

        }

        public DeviceDetailModel GetDetail(int id, int deviceGroupId)
        {
            if (id == -1)
            {
                string deviceGroupDescription;
                using (var scope = _dbContextScopeFactory.Create())
                {
                    var info =
                        scope.Get<DeviceGroup>()
                            .Where(x => x.DeviceGroupId == deviceGroupId)
                            .Select(x => new { x.Description, x.AccountId })
                            .FirstOrDefault();
                    if (info == null)
                    {
                        throw new Exception("Device group not found: " + deviceGroupId);
                    }
                    if (info.AccountId != Helper.AccountId)
                    {
                        throw new Exception("You do not have permission to access the device group: " + deviceGroupId);
                    }
                    deviceGroupDescription = info.Description;

                    return new DeviceDetailModel
                    {
                        DeviceId = -1,
                        DeviceGroupId = deviceGroupId,
                        DeviceGroupDescription = deviceGroupDescription,
                        ApiKey = AgentTokenService.GenerateApiKey(),
                        Description = "",
                        Title = "<new>"
                    };
                }

            }



            DeviceDetailModel device;
            using (var scope = _dbContextScopeFactory.Create())
            {
                device = scope.Get<Device>()
                        .Where(x => x.DeviceId == id && (Authorizer.IsAdministrator || x.AccountId == Helper.AccountId))
                        .ProjectToDeviceDetailModel()
                        .FirstOrDefault();
                if (device == null)
                {
                    throw new Exception($"Device with the id of - {id} - was not found");
                }

            }
            device.Title = device.Description;

            return device;
        }

        public int Insert(DeviceDetailModel model)
        {
            var description = model.Description.Trim();
            if (description.Length < 3)
            {
                throw new Exception("Device name must be at least 3 characters");
            }
            using (var scope = _dbContextScopeFactory.Create())
            {
                var d =
                    scope.Get<Device>()
                        .FirstOrDefault(x => x.AccountId == Helper.AccountId && x.Description == description);
                if (d != null)
                {
                    throw new Exception("Device already exists");
                }
                var dg = scope.Get<DeviceGroup>().FirstOrDefault(x => x.DeviceGroupId == model.DeviceGroupId);
                if (dg == null)
                {
                    throw new Exception("Device group does not exist: " + model.DeviceGroupId);
                }
                if (dg.AccountId != Helper.AccountId)
                {
                    throw new Exception("You do not have permission to create a device on this device group.");
                }
                // TODO check devicd group filters

                // update the fields
                var device = new Device
                {
                    AccountId = Helper.AccountId,
                    Description = description,
                    DeviceGroupId = model.DeviceGroupId,
                    ApiKey = string.IsNullOrWhiteSpace(model.ApiKey) ? AgentTokenService.GenerateApiKey() : model.ApiKey,
                    WebUrl = model.WebUrl,
                    Notes = model.Notes
                };
                scope.Add(device);
                scope.SaveChanges();
                return device.DeviceId;
            }
        }

        public void Update(DeviceDetailModel model)
        {
            model.Description = model.Description.Trim();
            if (model.Description.Length < 3)
            {
                throw new Exception("Device name must be at least 3 characters");
            }
            using (var scope = _dbContextScopeFactory.Create())
            {
                var device = scope
                    .Get<Device>().FirstOrDefault(d => d.DeviceId == model.DeviceId);
                if (device == null)
                {
                    throw new Exception("Device not found: "+model.DeviceId);
                }
                if (device.AccountId != Helper.AccountId)
                {
                    throw new Exception("You do not have permission to modify this device");
                }
                // TODO need to handle filtered device groups

                if (device.Description != model.Description)
                {
                    var count =
                        scope.Get<Device>()
                            .Count(x => x.DeviceGroupId == model.DeviceGroupId && x.Description == model.Description);
                    if (count != 0)
                    {
                        throw new Exception("device name already exists");
                    }
                }
                // update the fields — null means "keep existing key" (key is never sent back to the browser)
                if (!string.IsNullOrWhiteSpace(model.ApiKey))
                    device.ApiKey = model.ApiKey;
                device.Description = model.Description;
                device.WebUrl = model.WebUrl;
                device.Notes = model.Notes;

                scope.SaveChanges();
            }

        }

        public void Delete(int accountId, int id)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var device =
                    scope.Get<Device>().FirstOrDefault(x => x.AccountId == accountId && x.DeviceId == id);
                if (device == null)
                {
                    throw new Exception("Device not found - " + id);
                }
                scope.RemoveDevice(id);
                scope.SaveChanges();
            }
        }

        public void Pause(int accountId, int deviceId, int minutes)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var device = scope.Get<Device>().FirstOrDefault(x => x.AccountId == accountId && x.DeviceId == deviceId);
                if (device == null)
                    throw new Exception($"Device not found - accountId={accountId}, deviceId={deviceId}");
                device.PauseUntilDateTime = DateTime.Now.AddMinutes(minutes);
                scope.SaveChanges();
            }
        }

        public void Resume(int accountId, int deviceId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var device = scope.Get<Device>().FirstOrDefault(x => x.AccountId == accountId && x.DeviceId == deviceId);
                if (device == null)
                    throw new Exception($"Device not found - accountId={accountId}, deviceId={deviceId}");
                device.PauseUntilDateTime = null;
                scope.SaveChanges();
            }
        }

        public class SshInfo
        {
            public string Server { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }

        public SshInfo GetIloInfoForDevice(int deviceId)
        {
            return null;
        }

        public SshInfo GetSshInfoForDevice(int deviceId)
        {
            return null;
        }

        public string GetFullName(int deviceId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var result = scope.Get<Device>().Where(x => x.DeviceId == deviceId)
                    .Select(x => new
                    {
                        GroupDescription = x.DeviceGroup.Description,
                        DeviceDescription = x.Description
                    })
                    .First();
                return result.GroupDescription + " > " + result.DeviceDescription;
            }
        }

        public void SetConfigXml(int deviceId, string configXml)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var device = scope.Get<Device>().Where(x => x.DeviceId == deviceId).Select(x => x).First();
                device.ConfigXml = configXml;
                scope.SaveChanges();
            }
        }
    }
}