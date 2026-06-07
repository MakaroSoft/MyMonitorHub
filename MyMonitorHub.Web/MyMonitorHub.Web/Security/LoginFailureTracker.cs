using Microsoft.Extensions.Caching.Memory;

namespace MyMonitorHub.Web.Security
{
    /// <summary>
    /// Tracks failed user login attempts and enforces temporary lockout after repeated failures.
    /// </summary>
    public class LoginFailureTracker
    {
        private const int MaxFailures = 5;
        private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly IMemoryCache _cache;

        public LoginFailureTracker(IMemoryCache cache)
        {
            _cache = cache;
        }

        public static string NormalizeEmail(string email) =>
            email.Trim().ToLowerInvariant();

        public bool IsLockedOut(string emailKey) => _cache.TryGetValue(LockoutKey(emailKey), out _);

        public void RecordFailure(string emailKey)
        {
            var countKey = CountKey(emailKey);
            var count = _cache.TryGetValue(countKey, out int existing) ? existing + 1 : 1;
            _cache.Set(countKey, count, FailureWindow);

            if (count >= MaxFailures)
                _cache.Set(LockoutKey(emailKey), true, LockoutDuration);
        }

        public void ClearFailures(string emailKey)
        {
            _cache.Remove(CountKey(emailKey));
            _cache.Remove(LockoutKey(emailKey));
        }

        private static string CountKey(string key) => $"login:fail:{key}";
        private static string LockoutKey(string key) => $"login:lock:{key}";
    }
}
