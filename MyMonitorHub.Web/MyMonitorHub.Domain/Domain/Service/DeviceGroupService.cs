using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MyMonitorHub.Domain.Util;
using MyMonitorHub.Domain.Dto;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Models;
using MyMonitorHub.Domain.Security;
using DeviceGroup = MyMonitorHub.Domain.Entity.DeviceGroup;
using MonthlyReportModel = MyMonitorHub.Domain.Dto.MonthlyReportModel;

namespace MyMonitorHub.Domain.Service
{
    /// <summary>
    ///     Summary description for Pages
    /// </summary>
    public class DeviceGroupService
    {
        private readonly IDbContextScopeFactory _dbContextScopeFactory;

        public DeviceGroupService(IDbContextScopeFactory dbContextScopeFactory)
        {
            _dbContextScopeFactory = dbContextScopeFactory;
        }

        public void Update(DeviceGroupDetailModel deviceGroup)
        {
            deviceGroup.Description = deviceGroup.Description.Trim();
            if (deviceGroup.Description.Length < 3)
            {
                throw new Exception("Device group name must be at least 3 characters");
            }
            using (var scope = _dbContextScopeFactory.Create())
            {
                var dg =
                    scope.Get<DeviceGroup>()
                        .FirstOrDefault(
                            x => x.DeviceGroupId == deviceGroup.DeviceGroupId && x.AccountId == deviceGroup.AccountId);
                if (dg == null)
                {
                    throw new Exception("Device group not found: " + deviceGroup.DeviceGroupId);
                }
                if (dg.Description != deviceGroup.Description)
                {
                    var count =
                        scope.Get<DeviceGroup>()
                            .Count(x => x.PageId == deviceGroup.PageId && x.Description == deviceGroup.Description);
                    if (count != 0)
                    {
                        throw new Exception("Device group name already exists");
                    }
                }
                dg.Description = deviceGroup.Description;
                dg.emailsForClosedSRs = deviceGroup.emailsForClosedSRs;
                dg.Notes = deviceGroup.Notes;
                scope.SaveChanges();
            } // using
        }

        public List<TextValuePair> GetKeyValuePairs()
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                if (!Helper.IsAdministrator)
                {
                    return scope.Get<DeviceGroup>().Where(x => x.AccountId == Helper.AccountId)
                        .Select(
                            x => new {description = x.Description, deviceGroupId = x.DeviceGroupId, pageId = x.PageId})
                        .OrderBy(x => x.description)
                        .AsEnumerable()
                        .Select(
                            x =>
                                new TextValuePair
                                {
                                    Text = x.description,
                                    Value = x.deviceGroupId.ToString(CultureInfo.InvariantCulture)
                                })
                        .ToList();
                }
                return scope.Get<DeviceGroup>()
                    .Select(x => new {description = x.Description, deviceGroupId = x.DeviceGroupId})
                    .OrderBy(x => x.description)
                    .AsEnumerable()
                    .Select(
                        x =>
                            new TextValuePair
                            {
                                Text = x.description,
                                Value = x.deviceGroupId.ToString(CultureInfo.InvariantCulture)
                            })
                    .ToList();
            } // using
        }

        public int[] GetAllowed()
        {
            // null indicates that there is no device group security or there is but you can see them all anyways

            if (Helper.IsAdministrator) return null;
            using (var scope = _dbContextScopeFactory.Create())
            {
                var count = scope.Get<DeviceGroup>().Count(x => x.AccountId == Helper.AccountId);

                var allowed = scope.Get<DeviceGroup>().Where(x => x.AccountId == Helper.AccountId)
                    .Select(x => new {deviceGroupId = x.DeviceGroupId, pageId = x.PageId})
                    .AsEnumerable()
                    .Select(x => x.deviceGroupId)
                    .ToArray();
                if (allowed.Length == count)
                {
                    return null;
                }
                return allowed;
            }
        }

        private List<MonthlyReportModel> ByPage(int accountId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var test = scope.Get<DeviceGroup>().Where(x => x.AccountId == accountId)
                    .OrderBy(x => x.Page.Description).ThenBy(x => x.Page.PageId).ThenBy(x => x.Description).ToList();

                return (from a in test
                    group a by new {pageName = a.Page.Description, a.Page.PageId}
                    into g
                    select new MonthlyReportModel
                    {
                        PageId = g.Key.PageId,
                        PageName = g.Key.pageName,
                        Companies =
                            g.Select(
                                x =>
                                    new MonthlyReportModelCompany
                                    {
                                        DeviceGroupId = x.DeviceGroupId,
                                        Name = x.Description,
                                        IsGenerated = false,
                                        IsEmailed = false,
                                        Path = ""
                                    }).ToList()
                    }).ToList();
            }
        }

        public List<MonthlyReportModel> ByPage(int year, int month)
        {
            using (_dbContextScopeFactory.Create())
            {
                var pages = ByPage(Helper.AccountId);
                var emails = new MonthlyReportService(_dbContextScopeFactory).AllEmailed(Helper.AccountId, year, month);


                foreach (var page in pages)
                {
                    foreach (var company in page.Companies)
                    {
                        var path = GetPath(company.DeviceGroupId, year, month);
                        if (!String.IsNullOrEmpty(path))
                        {
                            company.IsGenerated = true;
                            company.Path = path;
                            company.IsEmailed = emails.Contains(company.DeviceGroupId);
                        }
                    }
                }

                return pages;
            }
        }

        public string GetCompanyName(int deviceGroupId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                return
                    scope.Get<DeviceGroup>()
                        .Where(x => x.DeviceGroupId == deviceGroupId)
                        .Select(x => x.Description)
                        .FirstOrDefault();
            }
        }

        public class DeviceGroupColumns
        {
            public int DeviceGroupId { get; set; }
            public int AccountId { get; set; }
            // ReSharper disable once InconsistentNaming
            public string emailsForClosedSRs { get; set; }
            public string Description { get; set; }
        }

        public DeviceGroupColumns GetData(int deviceGroupId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                return
                    scope.Get<DeviceGroup>()
                        .Where(x => x.DeviceGroupId == deviceGroupId && (Authorizer.IsAdministrator || x.AccountId == Helper.AccountId))
                        .ProjectToDeviceGroupColumns()
                        .FirstOrDefault();
            }
        }

        private string GetPath(int deviceGroupId, int year, int month)
        {
            var physicalPath = System.IO.Path.Combine(Helper.WebRootPath, "Get", deviceGroupId.ToString(), "Monthly");
            if (!Directory.Exists(physicalPath))
            {
                Directory.CreateDirectory(physicalPath);
            }
            var name = year + "-" + month.ToString("D2");
            physicalPath = System.IO.Path.Combine(Helper.WebRootPath, "repository", deviceGroupId.ToString(), "Monthly", name + ".pdf");
            var urlAbsolutePath = "/repository/" + deviceGroupId + "/Monthly/" + name + ".pdf";
            return File.Exists(physicalPath) ? urlAbsolutePath : "";
        }

        public void Delete(int accountId, int id)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var dg =
                    scope.Get<DeviceGroup>().FirstOrDefault(x => x.AccountId == accountId && x.DeviceGroupId == id);
                if (dg == null)
                {
                    throw new Exception("Device group not found - " + id);
                }
                var count = scope.Get<Device>().Count(x => x.DeviceGroupId == id);
                if (count != 0)
                {
                    throw new Exception("You must delete all the devices from the device group first");
                }
                scope.Delete(dg);
                scope.SaveChanges();
            }
        }

        public int Insert(DeviceGroupDetailModel model)
        {
            var description = model.Description.Trim();
            if (description.Length < 3)
            {
                throw new Exception("Device group name must be at least 3 characters");
            }
            using (var scope = _dbContextScopeFactory.Create())
            {
                var dg = scope.Get<DeviceGroup>().FirstOrDefault(x => x.AccountId == Helper.AccountId && x.Description == description);
                if (dg != null)
                {
                    throw new Exception("Device group already exists");
                }
                var p = scope.Get<Page>().FirstOrDefault(x => x.PageId == model.PageId);
                if (p == null)
                {
                    throw new Exception("Page does not exist: "+model.PageId);
                }
                if (p.AccountId != Helper.AccountId)
                {
                    throw new Exception("You do not have permission to create a device group on this page.");
                }

                // update the fields
                var deviceGroup = new DeviceGroup
                {
                    AccountId = Helper.AccountId,
                    PageId = model.PageId,
                    Description = description,
                    Notes = model.Notes,
                    emailsForClosedSRs = model.emailsForClosedSRs
                };
                scope.Add(deviceGroup);
                scope.SaveChanges();
                return deviceGroup.DeviceGroupId;
            }

        }
        public object Insert(int accountId, int pageId, string description)
        {
            throw new NotImplementedException();
        }

        public DeviceGroupDetailModel GetDetail(int id, int pageId)
        {
            if (id == -1)
            {
                using (var scope = _dbContextScopeFactory.Create())
                {
                    var info =
                        scope.Get<Page>()
                            .Where(x => x.PageId == pageId)
                            .Select(x => new {x.Description, x.AccountId})
                            .FirstOrDefault();
                    if (info == null)
                    {
                        throw new Exception("Page not found: "+pageId);
                    }
                    if (info.AccountId != Helper.AccountId)
                    {
                        throw new Exception("You do not have permission to access the page: "+pageId);
                    }
                    var pageDescription = info.Description;

                    return new DeviceGroupDetailModel
                    {
                        DeviceGroupId = -1,
                        PageId = pageId,
                        PageDescription = pageDescription,
                        Description = "",
                        Title = "<new>"
                    };
                }

            }

            using (var scope = _dbContextScopeFactory.Create())
            {
                var model = scope.Get<DeviceGroup>()
                    .Where(dg => dg.DeviceGroupId == id && (Authorizer.IsAdministrator || dg.AccountId == Helper.AccountId))
                    .ProjectToDeviceGroupDetailModel();
                var data = model.FirstOrDefault();
                if (data == null)
                {
                    throw new Exception($"Device group with the id of - {id} - was not found");
                }
                data.Title = data.Description;
                return data;
            }
        }
    }
}