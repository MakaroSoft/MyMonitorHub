using System.ComponentModel.DataAnnotations;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Service;
using MyMonitorHub.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MyMonitorHub.Web.Controllers
{
    [ApiController]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting(AgentAuthRateLimitPolicies.ByIp)]
    public class AgentAuthController : ControllerBase
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<AgentAuthController> _logger;
        private readonly IConfiguration _configuration;
        private readonly AgentAuthFailureTracker _failureTracker;

        public AgentAuthController(
            IDbContextScopeFactory contextScopeFactory,
            ILoggerFactory loggerFactory,
            IConfiguration configuration,
            AgentAuthFailureTracker failureTracker)
        {
            _contextScopeFactory = contextScopeFactory;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<AgentAuthController>();
            _configuration = configuration;
            _failureTracker = failureTracker;
        }

        /// <summary>
        /// Agent presents its API Key to obtain an access token + refresh token pair.
        /// The API Key is only transmitted here — all subsequent requests use the short-lived access token.
        /// </summary>
        [HttpPost("api/agent/auth/token")]
        public IActionResult GetToken([FromBody] TokenRequest request)
        {

            var deviceKey = $"device:{request.AccountId}:{request.DeviceId}";
            if (_failureTracker.IsLockedOut(deviceKey))
            {
                _logger.LogWarning(
                    "Agent token exchange blocked by lockout. IP={Ip} AccountId={AccountId} DeviceId={DeviceId}",
                    ClientIp, request.AccountId, request.DeviceId);
                return StatusCode(StatusCodes.Status429TooManyRequests, "Too many failed authentication attempts.");
            }

            var tokenService = new AgentTokenService(_contextScopeFactory, _loggerFactory, _configuration);
            var pair = tokenService.IssueTokens(request.AccountId, request.DeviceId, request.ApiKey);

            if (pair == null)
            {
                _failureTracker.RecordFailure(deviceKey);
                _logger.LogWarning(
                    "Agent token exchange failed. IP={Ip} AccountId={AccountId} DeviceId={DeviceId}",
                    ClientIp, request.AccountId, request.DeviceId);
                return Unauthorized();
            }

            _failureTracker.ClearFailures(deviceKey);
            return Ok(new TokenResponse { AccessToken = pair.AccessToken, RefreshToken = pair.RefreshToken });
        }

        /// <summary>
        /// Agent presents its refresh token to obtain a new access token + new refresh token.
        /// The old refresh token is revoked. If a revoked token is re-presented, the entire token
        /// family is revoked (theft detection).
        /// </summary>
        [HttpPost("api/agent/auth/refresh")]
        public IActionResult Refresh([FromBody] RefreshRequest request)
        {

            var ipKey = $"ip:{ClientIp}";
            if (_failureTracker.IsLockedOut(ipKey))
            {
                _logger.LogWarning("Agent refresh blocked by lockout. IP={Ip}", ClientIp);
                return StatusCode(StatusCodes.Status429TooManyRequests, "Too many failed authentication attempts.");
            }

            var tokenService = new AgentTokenService(_contextScopeFactory, _loggerFactory, _configuration);
            var pair = tokenService.RotateRefreshToken(request.RefreshToken);

            if (pair == null)
            {
                _failureTracker.RecordFailure(ipKey);
                _logger.LogWarning("Agent refresh failed. IP={Ip}", ClientIp);
                return Unauthorized();
            }

            _failureTracker.ClearFailures(ipKey);
            return Ok(new TokenResponse { AccessToken = pair.AccessToken, RefreshToken = pair.RefreshToken });
        }

        private string ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        public class TokenRequest
        {
            [Range(1, int.MaxValue, ErrorMessage = "AccountId must be a positive integer.")]
            public int AccountId { get; set; }

            [Range(1, int.MaxValue, ErrorMessage = "DeviceId must be a positive integer.")]
            public int DeviceId { get; set; }

            [Required]
            [StringLength(512, MinimumLength = 1)]
            public string ApiKey { get; set; } = string.Empty;
        }

        public class RefreshRequest
        {
            [Required]
            [StringLength(2048, MinimumLength = 1)]
            public string RefreshToken { get; set; } = string.Empty;
        }

        public class TokenResponse
        {
            public string AccessToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
        }
    }
}
