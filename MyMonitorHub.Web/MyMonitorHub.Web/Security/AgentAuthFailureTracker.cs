using Microsoft.Extensions.Caching.Memory;

namespace MyMonitorHub.Web.Security
{
    /// <summary>
    /// Tracks failed agent auth attempts and enforces temporary lockout after repeated failures.
    /// </summary>
    public class AgentAuthFailureTracker
    {
        private const int MaxFailures = 5;
        private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly IMemoryCache _cache;

        public AgentAuthFailureTracker(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool IsLockedOut(string key) => _cache.TryGetValue(LockoutKey(key), out _);

        public void RecordFailure(string key)
        {
            var countKey = CountKey(key);
            var count = _cache.TryGetValue(countKey, out int existing) ? existing + 1 : 1;
            _cache.Set(countKey, count, FailureWindow);

            if (count >= MaxFailures)
                _cache.Set(LockoutKey(key), true, LockoutDuration);
        }

        public void ClearFailures(string key)
        {
            _cache.Remove(CountKey(key));
            _cache.Remove(LockoutKey(key));
        }

        private static string CountKey(string key) => $"agent-auth:fail:{key}";
        private static string LockoutKey(string key) => $"agent-auth:lock:{key}";
    }

}
