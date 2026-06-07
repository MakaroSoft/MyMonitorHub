using System;
using MyMonitorHub.Common.WebApi;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MyMonitorHub.Web.Controllers
{
    [ApiController]
    [IgnoreAntiforgeryToken]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class WebServiceController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDbContextScopeFactory _contextScopeFactory;

        public WebServiceController(IDbContextScopeFactory contextScopeFactory, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<WebServiceController>();
            _contextScopeFactory = contextScopeFactory;
        }

        [HttpPost("api/WebService/Pause")]
        public ReturnPacket Pause([FromBody] PauseResumeData pauseResumeData)
        {
            var (accountId, deviceId) = GetClaims();
            _logger.LogDebug("Pause({0},{1},{2})", accountId, deviceId, pauseResumeData.Minutes);
            try
            {
                new DeviceService(_contextScopeFactory, _loggerFactory).Pause(accountId, deviceId, pauseResumeData.Minutes);
                return new ReturnPacket { Success = true };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Pause failed for accountId={AccountId} deviceId={DeviceId}", accountId, deviceId);
                return new ReturnPacket { Success = false, Exception = new ServiceException { Message = "An error occurred while pausing the device." } };
            }
        }

        [HttpPost("api/WebService/Resume")]
        public ReturnPacket Resume()
        {
            var (accountId, deviceId) = GetClaims();
            _logger.LogDebug("Resume({0},{1})", accountId, deviceId);
            try
            {
                new DeviceService(_contextScopeFactory, _loggerFactory).Resume(accountId, deviceId);
                return new ReturnPacket { Success = true };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Resume failed for accountId={AccountId} deviceId={DeviceId}", accountId, deviceId);
                return new ReturnPacket { Success = false, Exception = new ServiceException { Message = "An error occurred while resuming the device." } };
            }
        }

        [HttpPost("api/WebService/EventProcessor")]
        public IActionResult EventProcessor([FromBody] EventPacket packet)
        {
            var (accountId, deviceId) = GetClaims();
            try
            {
                _logger.LogDebug("EventProcessor({0},{1})", accountId, deviceId);
                new EventService(_contextScopeFactory, _loggerFactory).WriteEvents(accountId, deviceId, packet);
                return Ok(true);
            }
            catch (NoLogException)
            {
                return BadRequest("Event logging is not enabled for this device.");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "EventProcessor failed for accountId={AccountId} deviceId={DeviceId}", accountId, deviceId);
                return BadRequest("An error occurred while processing events.");
            }
        }

        private (int accountId, int deviceId) GetClaims()
        {
            var accountId = int.Parse(User.FindFirst("accountId")!.Value);
            var deviceId = int.Parse(User.FindFirst("deviceId")!.Value);
            return (accountId, deviceId);
        }

        public class PauseResumeData { public int Minutes; }
        public class ReturnPacket { public bool Success; public ServiceException? Exception; }
        public class ServiceException { public string? Message; }
    }
}
