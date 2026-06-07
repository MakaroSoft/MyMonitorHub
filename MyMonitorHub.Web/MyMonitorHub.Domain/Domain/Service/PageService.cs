using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MyMonitorHub.Domain.Util;
using MyMonitorHub.Domain.BO.Transfer;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Exceptions;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Models;
using MyMonitorHub.Domain.Security;
namespace MyMonitorHub.Domain.Service
{
    public class TreeNode
    {
        public List<TreeNode> ChildNodes = new List<TreeNode>();

        public TreeNode(string name)
        {
            Name = name;
        }

        public TreeNode(string name, int pageId)
        {
            Name = name;
            PageId = pageId;
        }

        public string Name { get; set; }
        public int PageId { get; set; }
    }

    public class PageService
    {
        private readonly Dictionary<string, List<Page>> _pagesLookup = new Dictionary<string, List<Page>>();
        private readonly IDbContextScopeFactory _contextScopeFactory;
        public PageService(IDbContextScopeFactory contextScopeFactory)
        {
            _contextScopeFactory = contextScopeFactory;
        }

        public TreeNode GetPagesForAccount(int accountId)
        {
            return GetPagesForAccount(true, accountId);
        }

        public TreeNode GetPagesForAccount(bool addUrl, int accountId)
        {
            using (var scope = _contextScopeFactory.Create())
            {
                IQueryable<Page> pages = from p in scope.Get<Page>().Where(x => x.AccountId == accountId)
                    orderby p.ParentId, p.ChildIndex
                    select p;

                foreach (var pRow in pages)
                {
                    string key;
                    if (pRow.ParentId == null)
                    {
                        key = "<null>";
                    }
                    else
                    {
                        key = pRow.ParentId.ToString();
                    }

                    List<Page> list;
                    if (_pagesLookup.ContainsKey(key))
                    {
                        list = _pagesLookup[key];
                    }
                    else
                    {
                        list = new List<Page>();
                        _pagesLookup.Add(key, list);
                    }
                    list.Add(pRow);
                }

                var tn = new TreeNode("Root");
                Recurse(tn, "<null>");

                return tn;
            } // using
        }

        private void Recurse(TreeNode tn, String key)
        {
            var children = _pagesLookup[key];
            if (children != null)
            {
                foreach (var page in children)
                {
                    var childTn = new TreeNode(page.Description, page.PageId);
                    tn.ChildNodes.Add(childTn);
                    var childKey = page.PageId.ToString(CultureInfo.InvariantCulture);
                    if (childKey == "")
                    {
                        childKey = "<null>";
                    }
                    if (_pagesLookup.ContainsKey(childKey))
                    {
                        Recurse(childTn, childKey);
                    }
                }
            }
        }

        public PageTransfer GetPageInformation(int pageId, int deviceId, bool readOnly)
        {
            var page = new PageTransfer();

            using (var scope = _contextScopeFactory.Create())
            {
                // get all the item columns for the page
                var categoryNames = (from i in scope.Get<Item>()
                    join c in scope.Get<Category>() on i.CategoryId equals c.CategoryId
                    join d in scope.Get<Device>() on i.DeviceId equals d.DeviceId
                    join dg in scope.Get<DeviceGroup>() on d.DeviceGroupId equals dg.DeviceGroupId
                    where dg.PageId == pageId
                    select c).Distinct().ToArray();

                // set up the page headers
                page.ColumnHeaders = new ColumnHeaderTransfer[categoryNames.Count()];
                var headerIndex = 0;
                foreach (var nameRow in categoryNames)
                {
                    var columnHeader = new ColumnHeaderTransfer();
                    page.ColumnHeaders[headerIndex++] = columnHeader;
                    columnHeader.Description = nameRow.Description;
                }

                page.DeviceGroups = new DeviceGroupTransfer[1];
                page.DeviceGroups[0] = new DeviceGroupTransfer();

                var deviceGroup = page.DeviceGroups[0];
                deviceGroup.Devices = new DeviceTransfer[1];

                var devices =
                    scope.Get<Device>().Where(x => x.DeviceId == deviceId && x.Deleted == false).ToArray();


                var deviceIndex = 0;
                foreach (var deviceRow in devices)
                {
                    var device = new DeviceTransfer();
                    deviceGroup.Devices[deviceIndex++] = device;
                    device.DeviceId = deviceRow.DeviceId;
                    device.Columns = new ColumnTransfer[categoryNames.Length];
                    device.Description = deviceRow.Description;

                    // for each column on the page 
                    var columnIndex = 0;
                    //Category[] categoryRows = deviceRow.Categories.ToArray(); // registered event groups for the device

                    foreach (var nameRow in categoryNames)
                    {
                        var column = new ColumnTransfer();
                        device.Columns[columnIndex++] = column;

                        // change db.items to namerow.items because items already cached in namerow
                        var items = new List<Item>();
                        foreach (var i in nameRow.Items)
                        {
                            if (i.DeviceId == deviceRow.DeviceId) items.Add(i);
                        }

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

                            var timespan = DateTime.Now - earliest;
                            if (timespan.TotalMinutes > 15)
                            {
                                code = "late";
                            }

                            if (readOnly)
                            {
                                column.Code = code;
                            }
                            else
                            {
                                var theFile = "~/App_Themes/sampleTheme/" + code + ".gif";
                                var img = "<img border='0' src='" + theFile + "' />";
                                var link = "<a href='CategoryDetail.aspx?deviceId=" + deviceRow.DeviceId +
                                              "&categoryName=" + nameRow.Description + "' >" + img + "</a>";

                                column.Code = link;
                            }
                        }
                        else
                        {
                            if (readOnly)
                            {
                                column.Code = "dot";
                            }
                            else
                            {
                                var theFile = "~/App_Themes/sampleTheme/dot.gif";
                                var img = "<img src='" + theFile + "' />";
                                column.Code = img;
                            }
                        } // end if
                    }
                }
            } // using

            return page;
        }


        public string GetName(int pageId)
        {
            using (var scope =_contextScopeFactory.Create())
            {
                var name =
                    scope.Get<Page>().Where(x => x.PageId == pageId).Select(x => x.Description).FirstOrDefault();
                if (name == null)
                {
                    return "unknown";
                }
                return name;
            }

        }

        public object GetPages()
        {
            List<PageModel> data;
            using (var scope = _contextScopeFactory.Create())
            {
                data = scope.Get<Page>().Where(x => x.AccountId == Helper.AccountId).ProjectToPageModel().ToList();

            }
            return new PageMaintModel
            {
                Pages = data
            };
        }

        public PageDetailModel GetPage(int id)
        {
            if (id == -1)
            {
                return new PageDetailModel
                {
                    PageId = -1,
                    Description = "",
                    Title = "<new>"
                };
            }
            PageDetailModel data;
            using (var scope = _contextScopeFactory.Create())
            {
                var pages = scope.Get<Page>().Where(x => x.PageId == id);
                if (!Authorizer.IsAdministrator)
                {
                    // only view page from your own account
                    pages = pages.Where(x => x.AccountId == Helper.AccountId);
                }
                data = pages.ProjectToPageDetailModel().FirstOrDefault();
                if (data == null)
                {
                    throw new Exception($"Page with the id of - {id} - was not found");
                }
                data.Title = data.Description;
            }
            data.DeviceGroups = data.DeviceGroups.OrderBy(x => x.Description).ToList();
            return data;
        }

        public int Insert(int accountId, string description)
        {
            description = description.Trim();
            if (description.Length < 3)
            {
                throw new Exception("Page name must be at least 3 characters");
            }
            using (var scope = _contextScopeFactory.Create())
            {
                var p = scope.Get<Page>().FirstOrDefault(x => x.AccountId == accountId && x.Description == description);
                if (p != null)
                {
                    throw new Exception("Page already exists");
                }
                // update the fields
                var page = new Page
                {
                    AccountId = accountId,
                    Description = description
                };
                scope.Add(page);
                scope.SaveChanges();
                return page.PageId;
            }

        }

        public void Delete(int accountId, int pageId)
        {
            using (var scope = _contextScopeFactory.Create())
            {
                var page = scope.Get<Page>().FirstOrDefault(x => x.AccountId == accountId && x.PageId == pageId);
                if (page == null)
                {
                    throw new Exception("Page not found - "+pageId);
                }
                var count = scope.Get<DeviceGroup>().Count(x => x.PageId == pageId);
                if (count != 0)
                {
                    throw new Exception("You must delete the device groups from this page first");
                }
                scope.Delete(page);
                scope.SaveChanges();
            }
        }

        public void Update(PageDetailModel model)
        {
            model.Description = model.Description.Trim();
            if (model.Description.Length < 3)
            {
                throw new Exception("Page description must be at least 3 characters");
            }
            using (var scope = _contextScopeFactory.Create())
            {
                var page = scope.Get<Page>().FirstOrDefault(x => x.PageId == model.PageId);
                if (page == null)
                {
                    throw new Exception("Page not found");
                }
                if (page.AccountId != Helper.AccountId)
                {
                    throw new Exception("Permission denied");
                }
                if (page.Description != model.Description)
                {
                    var count =
                        scope.Get<Page>()
                            .Count(x => x.AccountId == Helper.AccountId && x.Description == model.Description);
                    if (count != 0)
                    {
                        throw new Exception("Page description already exists");
                    }
                }
                // update the fields
                page.Description = model.Description;

                scope.SaveChanges();
            }

        }


    } // class
} // namespace