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
    public class DeviceController : BaseController
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<DeviceController> _logger;
        private readonly WebSocketTicketFactory _webSocketTicketFactory;

        public DeviceController(IDbContextScopeFactory contextScopeFactory, WebSocketTicketFactory webSocketTicketFactory, ILoggerFactory loggerFactory)
            : base(contextScopeFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<DeviceController>();
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
                new DeviceService(_contextScopeFactory, _loggerFactory).Delete(Helper.AccountId, id);
                return Json(new { Status = "Success", Response = "Successfully deleted device", DeviceId = id });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Delete device {DeviceId} failed", id);
                return Json(new { Status = "Fail", Reason = "An error occurred while deleting the device." });
            }
        }

        public IActionResult Edit(int id = -1, int deviceGroupId = -1)
        {
            if (!Authorizer.Authorize(Permissions.CanViewDeviceMaint))
                throw new SecurityException(Permissions.CanViewDeviceMaint.FailMessage);

            var data = new DeviceService(_contextScopeFactory, _loggerFactory).GetDetail(id, deviceGroupId);
            if (data.DeviceId == -1)
                data.AccountId = Helper.AccountId;
            else
                data.ApiKey = null; // never send the stored key to the browser

            if (id != -1)
                data.ItemGroups = new DeviceService(_contextScopeFactory, _loggerFactory).GetItemGroups(id, Helper.AccountId);

            ViewBag.notesMd = MarkdownHelper.ToHtml(data.Notes);
            return View(data);
        }

        [HttpPost]
        public JsonResult DeleteItem(int id)
        {
            if (!Authorizer.Authorize(Permissions.CanEditDeviceMaint))
                throw new SecurityException(Permissions.CanEditDeviceMaint.FailMessage);
            try
            {
                new ItemService(_contextScopeFactory).DeleteItem(Helper.AccountId, id);
                return Json(new { Status = "Success" });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Delete item {ItemId} failed", id);
                return Json(new { Status = "Fail", Reason = "An error occurred while deleting the item." });
            }
        }

        [HttpPost]
        public JsonResult GenerateApiKey()
        {
            if (!Authorizer.Authorize(Permissions.CanEditDeviceMaint))
                throw new SecurityException(Permissions.CanEditDeviceMaint.FailMessage);
            return Json(new { ApiKey = AgentTokenService.GenerateApiKey() });
        }

        [HttpPost]
        public JsonResult Edit([FromBody] DeviceDetailModel model)
        {
            if (!Authorizer.Authorize(Permissions.CanEditDeviceMaint))
                throw new SecurityException(Permissions.CanEditDeviceMaint.FailMessage);
            try
            {
                if (model.DeviceId == -1)
                {
                    var deviceId = new DeviceService(_contextScopeFactory, _loggerFactory).Insert(model);
                    return Json(new { Status = "Success", Response = "Successfully Created Device", DeviceId = deviceId });
                }
                else
                {
                    new DeviceService(_contextScopeFactory, _loggerFactory).Update(model);
                    return Json(new { Status = "Success", Response = "Successfully Updated Device" });
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Save device {DeviceId} failed", model.DeviceId);
                return Json(new { Status = "Fail", DeviceName = model.Description, Reason = "An error occurred while saving the device." });
            }
        }

        public IActionResult Detail(int id, string? filter)
        {
            var model = new DeviceService(_contextScopeFactory, _loggerFactory).GetDeviceLayoutModel(id, filter, Request.PathBase.ToString());
            if (!string.IsNullOrEmpty(filter)) SetupCategory(id, filter);
            ViewBag.DeviceId = id;
            ViewBag.Ticket = _webSocketTicketFactory.Create().Ticket;

            if (filter == "Disk")
                ViewBag.ChartUrl = Url.Action("DiskGraph", "Reports") + "/";
            else if (filter == "HttpVortex")
                ViewBag.ChartUrl = Url.Action("ResponseGraph", "Reports") + "/";
            else if (filter == "CPU")
                ViewBag.ChartUrl = Url.Action("ResponseGraph", "Reports") + "/";

            var scheme = Request.IsHttps ? "wss" : "ws";
            var host = Request.Host.ToString();
            ViewBag.Location = $"{scheme}://{host}{Request.PathBase}";

            var isVms = model.DeviceGroupName.Contains("VMS");
            ViewBag.IsVms = isVms;

            ViewBag.ConnectionIcon = model.ConnectionStatus == ConnectionStatus.Connected
                ? "ms-icon16 ms-icon16-connected connectIcon"
                : "ms-icon16 ms-icon16-disconnected connectIcon";

            ViewBag.HideWebButton = string.IsNullOrEmpty(model.WebUrl) ? "style=\"display: none; \"" : "";
            ViewBag.HidePingButton = string.IsNullOrEmpty(model.WebPingUrl) ? "style=\"display: none; \"" : "";
            ViewBag.DisableServices = Authorizer.Authorize(Permissions.CanStopStartServices) ? "" : "disabled";

            return View(model);
        }

        public IActionResult DetailOnly(int id, string? filter)
        {
            var model = new DeviceService(_contextScopeFactory, _loggerFactory).GetDeviceLayoutModel(id, filter, Request.PathBase.ToString());
            return View(model);
        }

        private void SetupCategory(int deviceId, string categoryName)
        {
            using var scope = _contextScopeFactory.Create();
            var myItems = scope.Get<Item>()
                .Where(i => i.DeviceId == deviceId && i.Category.Description == categoryName)
                .Select(i => new { CatDescription = i.Category.Description, i.Description, i.ItemId })
                .ToList();

            var pairs = new List<TextValuePair>();
            foreach (var item in myItems)
            {
                if (item.CatDescription is "Disk" or "HttpVortex" or "CPU")
                {
                    if (ViewBag.CategoryName == null)
                    {
                        ViewBag.ReportName = item.CatDescription switch
                        {
                            "Disk" => "Disk",
                            "HttpVortex" => "Response Time",
                            _ => "CPU Usage"
                        };
                        ViewBag.Label = item.CatDescription switch
                        {
                            "Disk" => "Disk:",
                            "HttpVortex" => "Response time for:",
                            _ => "CPU Usage for:"
                        };
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
