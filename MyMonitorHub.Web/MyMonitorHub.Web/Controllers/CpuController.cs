using System;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MyMonitorHub.Web.Controllers
{
        [Authorize]
    public class CpuController : BaseController
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        public CpuController(IDbContextScopeFactory contextScopeFactory, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<CpuController>();
            _contextScopeFactory = contextScopeFactory;
        }

        [HttpPost]
        public JsonResult GetDay(int id, string date)
        {
            try
            {
                var data = new CpuService(_contextScopeFactory).GetDay(id, date);
                return Json(new { Status = "Success", Response = data, DeviceId = id });
            }
            catch (Exception e)
            {
                return Json(new { Status = "Fail", Reason = e.Message });
            }
        }

        [HttpPost]
        public JsonResult GetHour(int id, string date, int hour)
        {
            try
            {
                var data = new CpuService(_contextScopeFactory).GetHour(id, date, hour);
                return Json(new { Status = "Success", Response = data, DeviceId = id });
            }
            catch (Exception e)
            {
                return Json(new { Status = "Fail", Reason = e.Message });
            }
        }
    }
}
