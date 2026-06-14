using System;
using System.Linq;
using System.Threading.Tasks;
using MyMonitorHub.Domain.BO.Transfer;
using MyMonitorHub.Domain.Dto;
using MyMonitorHub.Domain.Exceptions;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Models;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Service;
using MyMonitorHub.Domain.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace MyMonitorHub.Web.Controllers
{
        [Authorize]
    public class ServiceRequestController : BaseController
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly WebSocketTicketFactory _webSocketTicketFactory;

        public ServiceRequestController(
            IDbContextScopeFactory contextScopeFactory,
            WebSocketTicketFactory webSocketTicketFactory,
            ILoggerFactory loggerFactory,
            ICompositeViewEngine viewEngine,
            ITempDataProvider tempDataProvider)
            : base(contextScopeFactory, viewEngine, tempDataProvider)
        {
            _loggerFactory = loggerFactory;
            _contextScopeFactory = contextScopeFactory;
            _webSocketTicketFactory = webSocketTicketFactory;
        }

        [HttpPost]
        public IActionResult SearchData(string sidx, string sord, int page, int itemsPerPage, SearchCriteria criteria)
        {
            var logger = _loggerFactory.CreateLogger<ServiceRequestController>();
            logger.LogInformation("SearchData criteria: Search={Search}, SrFrom={SrFrom}, SrTo={SrTo}, DateFrom={DateFrom}, DateTo={DateTo}, Account={Account}, AssignedTo={AssignedTo}, DeviceGroup={DeviceGroup}",
                criteria.Search, criteria.SrFrom, criteria.SrTo, criteria.DateFrom, criteria.DateTo,
                criteria.SelectedAccount, criteria.SelectedAssignedTo, criteria.SelectedDeviceGroup);

            if (criteria.Search == false)
            {
                HttpContext.Session.Remove(SessionKeys.SearchPageDefaults);
                return Json(new { TotalRecords = 0, DataObject = new { } });
            }

            var result = new ServiceRequestService(_contextScopeFactory, _loggerFactory).SearchData(sidx, sord, page, itemsPerPage, criteria);
            logger.LogInformation("SearchData result: TotalRecords={TotalRecords}", result.TotalRecords);

            HttpContext.Session.SetObject(SessionKeys.SearchPageDefaults, new PageDefaults
            {
                Criteria = criteria,
                Page = page,
                Rows = itemsPerPage,
                Sidx = sidx,
                Sord = sord
            });
            return Json(result);
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Duration = 0)]
        public IActionResult Search()
        {
            var searchView = new ServiceRequestService(_contextScopeFactory, _loggerFactory).GetSearchView();
            return View(searchView);
        }

        [HttpGet]
        public IActionResult Detail(int id)
        {
            var model = GetModel(id);
            ViewBag.Ticket = _webSocketTicketFactory.Create().Ticket;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Detail(int id, string btnSubmit, string notes)
        {
            try
            {
                return btnSubmit switch
                {
                    "Accept" => HandleAccept(id, notes),
                    "Save" => HandleSave(id, notes),
                    "Close" => await HandleClose(id, notes),
                    _ => throw new Exception("Unrecognized command: btnSubmit")
                };
            }
            catch (WarningException e)
            {
                return JsonWarning(e.Message);
            }
        }

        private IActionResult HandleAccept(int id, string notes)
        {
            new ServiceRequestService(_contextScopeFactory, _loggerFactory).Accept(id, notes);
            return Json(new { status = "Open", accept = false, save = true, close = true });
        }

        private async Task<IActionResult> HandleClose(int id, string notes)
        {
            var logger = _loggerFactory.CreateLogger<ServiceRequestController>();
            var emails = new ServiceRequestService(_contextScopeFactory, _loggerFactory).Close(id, notes);

            if (!string.IsNullOrEmpty(emails))
            {
                try
                {
                    // Render email body HTML in-process (SR is already closed so status shows "Closed")
                    var srClosedModel = new ServiceRequestService(_contextScopeFactory, _loggerFactory).SrClosed(id);
                    var emailBodyHtml = await RenderRazorViewToString("~/Views/Email/SrClosed.cshtml", srClosedModel);

                    // Render PDF HTML in-process
                    ILogoService logoService = new LogoService();
                    var srReportModel = new SrReportService(_contextScopeFactory, logoService).GetSrReportModel(id, Helper.AccountId);
                    ViewBag.ItemLayoutView = new ItemService(_contextScopeFactory).GetItemLayout(srReportModel.PageId, -1, srReportModel.DeviceId, false, Request.PathBase.ToString());
                    var pdfHtml = await RenderRazorViewToString("~/Views/Reports/ServiceRequest/HtmlFormatOfSrReport.cshtml", srReportModel);

                    new EmailService(_contextScopeFactory, _loggerFactory)
                        .AlertCustomerRequestIsClosed(emails, id, Helper.AccountId, emailBodyHtml, pdfHtml, Request.PathBase.ToString());
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to send SR-closed email for SR {Id}", id);
                }
            }

            return Json(new { status = "Closed", accept = false, save = true, close = false });
        }

        private IActionResult HandleSave(int id, string notes)
        {
            new ServiceRequestService(_contextScopeFactory, _loggerFactory).UpdateNotes(id, notes);
            return Json(new { });
        }

        private SrDetailView GetModel(int id)
        {
            var deviceConnections = AgentConnections.Current.GetStatus();

            var sRequest = new ServiceRequestService(_contextScopeFactory, _loggerFactory).GetSrWithPageAndUser(id);
            if (sRequest == null)
                throw new SecurityException("Service request is invalid or does not belong to your account.");

            var events = new EventService(_contextScopeFactory, _loggerFactory).GetEventsForSr(id);

            var history = (from ev in events
                           group ev by ev.Category + "> " + ev.SubCategory
                           into g
                           select new SrDetailViewHistory
                           {
                               Description = g.Key,
                               Events = g
                           }).ToList();

            ConnectionStatus? cs = null;
            var deviceConnection = deviceConnections.FirstOrDefault(dc => dc.DeviceId == sRequest.DeviceId);
            if (deviceConnection != null)
                cs = deviceConnection.Status;

            var model = new SrDetailView
            {
                ServiceRequestId = sRequest.ServiceRequestId,
                TimeStamp = sRequest.TimeStamp,
                Notes = sRequest.Notes,
                DeviceGroupName = "",
                DeviceDesc = sRequest.Device.Description,
                DeviceId = sRequest.DeviceId,
                UserName = "",
                ConnectionStatus = cs
            };

            if (sRequest.User != null)
                model.UserName = sRequest.User.Email;

            model.History = history;

            var group = sRequest.Device.DeviceGroup;
            if (group != null)
            {
                model.PageName = group.Page.Description;
                var myUrl = Url.Action("Detail", "Page", new { id = group.PageId }) + "#" + group.DeviceGroupId;
                model.DeviceGroupNameLink = $"<a class='underline' href='{myUrl}'>{group.Description}</a>";
                model.DeviceGroupName = group.Description;
                model.DeviceGroupId = group.DeviceGroupId;
            }
            else
            {
                model.PageName = "Device has been soft deleted and does not belong to a page";
                model.DeviceGroupName = "Device has been soft deleted and does not belong to a device group";
                model.DeviceGroupNameLink = model.DeviceGroupName;
            }

            if (sRequest.AssignedToId == null)
            {
                model.Status = "Open";
                ViewBag.btnAccept = true;
                ViewBag.btnSave = true;
                ViewBag.btnClose = false;
            }
            else if (sRequest.Status == "O")
            {
                model.Status = "Open";
                ViewBag.btnAccept = false;
                ViewBag.btnSave = true;
                ViewBag.btnClose = Helper.IsAdministrator || Helper.IsOwner || sRequest.AccountId == Helper.AccountId;
            }
            else
            {
                model.Status = "Closed";
                ViewBag.btnAccept = false;
                ViewBag.btnSave = true;
                ViewBag.btnClose = false;
            }

            if (!Authorizer.Authorize(Permissions.CanEditServiceRequests))
            {
                ViewBag.btnAccept = new { disabled = "disabled" };
                ViewBag.btnSave = new { disabled = "disabled" };
                ViewBag.btnClose = new { disabled = "disabled" };
            }

            if (group != null)
                ViewBag.ItemLayoutView = new ItemService(_contextScopeFactory).GetItemLayout(group.Page.PageId, -1, sRequest.DeviceId, false, Request.PathBase.ToString());

            return model;
        }
    }
}
