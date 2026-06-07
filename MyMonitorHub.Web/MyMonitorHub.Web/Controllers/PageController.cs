using System;
using System.Globalization;
using MyMonitorHub.Domain.Exceptions;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Models;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Service;
using MyMonitorHub.Domain.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MyMonitorHub.Web.Controllers
{
        [Authorize]
    public class PageController : BaseController
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;
        private readonly WebSocketTicketFactory _webSocketTicketFactory;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        public PageController(IDbContextScopeFactory contextScopeFactory, WebSocketTicketFactory webSocketTicketFactory, ILoggerFactory loggerFactory)
            : base(contextScopeFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<PageController>();
            _contextScopeFactory = contextScopeFactory;
            _webSocketTicketFactory = webSocketTicketFactory;
        }

        [HttpGet]
        public IActionResult List()
        {
            _logger.LogInformation("in /Page/List");
            if (!Authorizer.Authorize(Permissions.CanViewPageMaint))
                throw new SecurityException(Permissions.CanViewPageMaint.FailMessage);
            var data = new PageService(_contextScopeFactory).GetPages();
            return View(data);
        }

        public IActionResult Detail(int id)
        {
            if (!Authorizer.Authorize(Permissions.CanViewPage))
                throw new SecurityException(Permissions.CanViewPage.FailMessage);
            var data = new ItemService(_contextScopeFactory).GetItemLayout(id, -1, -1, false, Request.PathBase.ToString());
            ViewBag.Title = "Devices - " + new PageService(_contextScopeFactory).GetName(id);
            ViewBag.currentPage = id.ToString(CultureInfo.InvariantCulture);
            ViewBag.Ticket = _webSocketTicketFactory.Create().Ticket;
            return View(data);
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            if (!Authorizer.Authorize(Permissions.CanEditPageMaint))
                throw new SecurityException(Permissions.CanEditPageMaint.FailMessage);
            try
            {
                new PageService(_contextScopeFactory).Delete(Helper.AccountId, id);
                return Json(new { Status = "Success", Response = "Successfully deleted Page", PageId = id });
            }
            catch (Exception e)
            {
                return Json(new { Status = "Fail", Reason = e.Message });
            }
        }

        [HttpPost]
        public JsonResult Edit([FromBody] PageDetailModel model)
        {
            if (!Authorizer.Authorize(Permissions.CanEditPageMaint))
                throw new SecurityException(Permissions.CanViewPageMaint.FailMessage);
            try
            {
                if (model.PageId == -1)
                {
                    var pageId = new PageService(_contextScopeFactory).Insert(Helper.AccountId, model.Description);
                    return Json(new { Status = "Success", Response = "Successfully Created Page", PageId = pageId });
                }
                else
                {
                    new PageService(_contextScopeFactory).Update(model);
                    return Json(new { Status = "Success", Response = "Successfully Updated Page" });
                }
            }
            catch (Exception e)
            {
                return Json(new { Status = "Fail", DeviceName = model.Description, Reason = e.Message });
            }
        }

        public IActionResult Edit(int id = -1)
        {
            if (!Authorizer.Authorize(Permissions.CanViewPageMaint))
                throw new SecurityException(Permissions.CanViewPageMaint.FailMessage);
            var data = new PageService(_contextScopeFactory).GetPage(id);
            return View(data);
        }
    }
}
