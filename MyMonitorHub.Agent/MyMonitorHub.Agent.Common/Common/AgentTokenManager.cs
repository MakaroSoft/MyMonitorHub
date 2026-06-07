using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using Serilog;

namespace MyMonitorHub.Agent.Common
{
    /// <summary>
    /// Manages the agent's access token lifecycle: authenticates once with the API Key,
    /// caches the access token in memory, proactively refreshes before expiry,
    /// and persists the refresh token to MonitorProfile so it survives restarts.
    /// Thread-safe â€” multiple threads may call GetAccessToken concurrently.
    /// </summary>
    public class AgentTokenManager
    {
        private static readonly ILogger Logger = Log.ForContext<AgentTokenManager>();

        private readonly object _lock = new object();
        private string? _accessToken;
        private DateTime _accessTokenExpiry = DateTime.MinValue;

        private const int RefreshBufferMinutes = 5;

        /// <summary>
        /// Returns a valid access token, refreshing or re-authenticating as needed.
        /// Blocks the calling thread if a token fetch is in progress.
        /// </summary>
        public string GetAccessToken()
        {
            lock (_lock)
            {
                if (IsTokenValid())
                    return _accessToken!;

                var profile = MonitorProfile.Current;

                // Try refresh token first (API Key is not re-sent)
                if (!string.IsNullOrEmpty(profile.RefreshToken))
                {
                    var refreshed = TryRefresh(profile);
                    if (refreshed != null)
                    {
                        SetToken(refreshed);
                        return _accessToken!;
                    }
                    Logger.Warning("GetAccessToken> Refresh failed, falling back to API Key authentication.");
                }

                // Fall back to full authentication with API Key
                var pair = Authenticate(profile);
                SetToken(pair);
                return _accessToken!;
            }
        }

        private bool IsTokenValid()
        {
            return _accessToken != null && DateTime.UtcNow < _accessTokenExpiry.AddMinutes(-RefreshBufferMinutes);
        }

        private void SetToken(TokenPair pair)
        {
            _accessToken = pair.AccessToken;
            _accessTokenExpiry = ParseExpiry(pair.AccessToken);

            // Persist refresh token to disk so it survives service restarts
            var profile = MonitorProfile.Current;
            if (profile.RefreshToken != pair.RefreshToken)
            {
                profile.RefreshToken = pair.RefreshToken;
                profile.WriteRefreshToken();
            }
        }

        private static TokenPair Authenticate(MonitorProfile profile)
        {
            Logger.Information("GetAccessToken> Authenticating with API Key.");
            using var client = CreateHttpClient(profile.BaseAddress);

            var body = JsonSerializer.Serialize(new
            {
                accountId = profile.AccountId,
                deviceId = profile.DeviceId,
                apiKey = profile.ApiKey
            });

            var response = client.PostAsync(
                "api/agent/auth/token",
                new StringContent(body, Encoding.UTF8, "application/json")).Result;

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Authentication failed: {response.StatusCode} {response.ReasonPhrase}");

            var json = response.Content.ReadAsStringAsync().Result;
            return JsonSerializer.Deserialize<TokenPair>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? throw new Exception("Authentication response was empty");
        }

        private static TokenPair? TryRefresh(MonitorProfile profile)
        {
            Logger.Debug("GetAccessToken> Refreshing token.");
            try
            {
                using var client = CreateHttpClient(profile.BaseAddress);

                var body = JsonSerializer.Serialize(new { refreshToken = profile.RefreshToken });

                var response = client.PostAsync(
                    "api/agent/auth/refresh",
                    new StringContent(body, Encoding.UTF8, "application/json")).Result;

                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warning("GetAccessToken> Token refresh returned {0}", response.StatusCode);
                    return null;
                }

                var json = response.Content.ReadAsStringAsync().Result;
                return JsonSerializer.Deserialize<TokenPair>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                Logger.Error("GetAccessToken> Token refresh threw an exception: {Message}", ex.Message);
                return null;
            }
        }

        private static HttpClient CreateHttpClient(string baseAddress)
        {
            var client = new HttpClient { BaseAddress = new Uri(baseAddress) };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        /// <summary>Parses the JWT expiry from the payload without a full JWT library dependency.</summary>
        private static DateTime ParseExpiry(string jwt)
        {
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length != 3) return DateTime.UtcNow.AddMinutes(25);

                var payload = parts[1];
                // Pad base64url
                var mod = payload.Length % 4;
                if (mod != 0) payload += new string('=', 4 - mod);
                payload = payload.Replace('-', '+').Replace('_', '/');

                var bytes = Convert.FromBase64String(payload);
                var json = Encoding.UTF8.GetString(bytes);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("exp", out var exp))
                {
                    var unixSeconds = exp.GetInt64();
                    return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
                }
            }
            catch { /* fall through */ }

            return DateTime.UtcNow.AddMinutes(25);
        }

        private class TokenPair
        {
            public string AccessToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
        }
    }
}
