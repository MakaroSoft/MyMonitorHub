using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace MyMonitorHub.Web.Security
{
    public static class AgentAuthRateLimitPolicies
    {
        public const string ByIp = "agent-auth-by-ip";
        public const string LoginByIp = "login-by-ip";
        public const string AccountByIp = "account-by-ip";

        public static void AddAgentAuthPolicies(RateLimiterOptions options)
        {
            options.AddPolicy(ByIp, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        }

        public static void AddLoginPolicies(RateLimiterOptions options)
        {
            options.AddPolicy(LoginByIp, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        }

        /// <summary>
        /// Rate limit for Register, RecoverPassword, and ResetPassword.
        /// 10 requests per 15 minutes per IP — tight enough to block automation,
        /// generous enough for a legitimate user who retries a few times.
        /// </summary>
        public static void AddAccountPolicies(RateLimiterOptions options)
        {
            options.AddPolicy(AccountByIp, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0
                    }));
        }
    }
}
