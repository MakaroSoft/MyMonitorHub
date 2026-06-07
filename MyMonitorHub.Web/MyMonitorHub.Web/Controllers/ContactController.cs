using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Exceptions;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Models;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Service;
using MyMonitorHub.Domain.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

namespace MyMonitorHub.Web.Controllers
{
        [Authorize]
    public class ContactController : BaseController
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly WebSocketTicketFactory _webSocketTicketFactory;

        public ContactController(IDbContextScopeFactory contextScopeFactory, WebSocketTicketFactory webSocketTicketFactory, ILoggerFactory loggerFactory)
            : base(contextScopeFactory)
        {
            _loggerFactory = loggerFactory;
            _contextScopeFactory = contextScopeFactory;
            _webSocketTicketFactory = webSocketTicketFactory;
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            if (!Authorizer.Authorize(Permissions.CanEditDeviceMaint))
                throw new SecurityException(Permissions.CanEditDeviceMaint.FailMessage);
            try
            {
                new ContactService(_contextScopeFactory, _loggerFactory).Delete(Helper.AccountId, id);
                return Json(new { Status = "Success", Response = "Successfully deleted contact", ContactId = id });
            }
            catch (Exception e)
            {
                return Json(new { Status = "Fail", Reason = e.Message });
            }
        }

        public IActionResult Edit(int id = -1, int deviceGroupId = -1)
        {
            if (!Authorizer.Authorize(Permissions.CanViewDeviceGroupMaint))
                throw new SecurityException(Permissions.CanViewDeviceGroupMaint.FailMessage);
            ViewBag.PhoneTypes = new PhoneTypeService(_contextScopeFactory, _loggerFactory).Get();
            var data = new ContactService(_contextScopeFactory, _loggerFactory).GetDetail(id, deviceGroupId);
            return View(data);
        }

        [HttpPost]
        public JsonResult Edit([FromBody] ContactDetailModel model)
        {
            if (!Authorizer.Authorize(Permissions.CanEditDeviceGroupMaint))
                throw new SecurityException(Permissions.CanEditDeviceGroupMaint.FailMessage);
            try
            {
                if (model.ContactId == -1)
                {
                    var contactId = new ContactService(_contextScopeFactory, _loggerFactory).Insert(model);
                    return Json(new { Status = "Success", Response = "Successfully Created Contact", ContactId = contactId });
                }
                else
                {
                    new ContactService(_contextScopeFactory, _loggerFactory).Update(model);
                    return Json(new { Status = "Success", Response = "Successfully Updated Contact" });
                }
            }
            catch (Exception e)
            {
                return Json(new { Status = "Fail", ContactName = model.Name, Reason = e.Message });
            }
        }

        public IActionResult Detail(int id, string filter)
        {
            var model = new DeviceService(_contextScopeFactory, _loggerFactory).GetDeviceLayoutModel(id, filter, Request.PathBase.ToString());
            if (!string.IsNullOrEmpty(filter)) SetupCategory(id, filter);
            ViewBag.DeviceId = id;
            ViewBag.Ticket = _webSocketTicketFactory.Create().Ticket;
            return View("~/Views/Device/Detail.cshtml", model);
        }

        public IActionResult DetailOnly(int id, string filter)
        {
            var model = new DeviceService(_contextScopeFactory, _loggerFactory).GetDeviceLayoutModel(id, filter, Request.PathBase.ToString());
            return View("~/Views/Device/DetailOnly.cshtml", model);
        }

        private void SetupCategory(int deviceId, string categoryName)
        {
            using var scope = _contextScopeFactory.Create();
            var myItems = scope.Get<Item>()
                .Where(i => i.DeviceId == deviceId && i.Category.Description == categoryName)
                .ToList();
            var pairs = new List<TextValuePair>();
            foreach (var item in myItems)
            {
                if (item.Category.Description == "Disk" || item.Category.Description == "HttpVortex")
                {
                    if (ViewBag.CategoryName == null)
                    {
                        if (item.Category.Description == "Disk")
                        {
                            ViewBag.ReportName = "Disk";
                            ViewBag.Label = "Disk:";
                        }
                        else
                        {
                            ViewBag.ReportName = "Response Time";
                            ViewBag.Label = "Response time for:";
                        }
                    }
                    pairs.Add(new TextValuePair
                    {
                        Text = item.Description,
                        Value = item.ItemId.ToString(CultureInfo.InvariantCulture)
                    });
                }
            }
            ViewBag.Category = pairs;
        }
    }
}
