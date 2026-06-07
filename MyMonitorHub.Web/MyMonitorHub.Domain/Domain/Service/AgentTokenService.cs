using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace MyMonitorHub.Domain.Service
{
    public class TokenPair
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class AgentTokenService
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;
        private readonly ILogger _logger;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        private const int AccessTokenMinutes = 30;
        private const int RefreshTokenDays = 30;

        public AgentTokenService(IDbContextScopeFactory contextScopeFactory, ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _contextScopeFactory = contextScopeFactory;
            _logger = loggerFactory.CreateLogger<AgentTokenService>();
            _jwtSecret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
            _jwtIssuer = configuration["Jwt:Issuer"] ?? "mymonitorhub";
            _jwtAudience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
        }

        /// <summary>
        /// Validates an API Key against the database and issues an access + refresh token pair.
        /// Call this from the token endpoint after the caller presents their API Key.
        /// </summary>
        public TokenPair? IssueTokens(int accountId, int deviceId, string apiKey)
        {
            _logger.LogDebug("IssueTokens({0},{1})", accountId, deviceId);

            string? deviceName;
            using (var scope = _contextScopeFactory.Create())
            {
                deviceName = scope.Get<Device>()
                    .Where(x => x.AccountId == accountId
                             && x.DeviceId == deviceId
                             && x.ApiKey == apiKey
                             && x.Deleted == false)
                    .Select(x => x.Description)
                    .FirstOrDefault();
            }

            if (deviceName == null)
            {
                _logger.LogWarning("IssueTokens> API Key validation failed for accountId={0} deviceId={1}", accountId, deviceId);
                return null;
            }

            var accessToken = GenerateAccessToken(accountId, deviceId, deviceName);
            var (rawRefresh, hashRefresh) = GenerateRefreshToken();
            var familyId = Guid.NewGuid();

            using (var scope = _contextScopeFactory.Create())
            {
                scope.Add(new DeviceRefreshToken
                {
                    DeviceId = deviceId,
                    AccountId = accountId,
                    TokenHash = hashRefresh,
                    FamilyId = familyId,
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
                    Revoked = false
                });
                scope.SaveChanges();
            }

            return new TokenPair { AccessToken = accessToken, RefreshToken = rawRefresh };
        }

        /// <summary>
        /// Rotates a refresh token: revokes the old one, issues a new pair.
        /// If the incoming token was already used (theft), revokes the entire family.
        /// Returns null if the token is invalid or expired.
        /// </summary>
        public TokenPair? RotateRefreshToken(string incomingRefreshToken)
        {
            var incomingHash = Hash(incomingRefreshToken);

            using (var scope = _contextScopeFactory.Create())
            {
                var stored = scope.Get<DeviceRefreshToken>()
                    .FirstOrDefault(x => x.TokenHash == incomingHash);

                if (stored == null)
                {
                    _logger.LogWarning("RotateRefreshToken> Token not found");
                    return null;
                }

                if (stored.Revoked)
                {
                    // Token was already used — possible theft. Revoke the entire family.
                    _logger.LogWarning("RotateRefreshToken> Revoked token re-used (possible theft). Revoking family {FamilyId} (accountId={AccountId}, deviceId={DeviceId})", stored.FamilyId, stored.AccountId, stored.DeviceId);
                    RevokeFamily(scope, stored.FamilyId);
                    scope.SaveChanges();
                    return null;
                }

                if (stored.ExpiresAt <= DateTime.UtcNow)
                {
                    _logger.LogDebug("RotateRefreshToken> Token expired");
                    stored.Revoked = true;
                    scope.SaveChanges();
                    return null;
                }

                // Valid — revoke old, issue new in the same family
                stored.Revoked = true;

                var (rawRefresh, hashRefresh) = GenerateRefreshToken();
                scope.Add(new DeviceRefreshToken
                {
                    DeviceId = stored.DeviceId,
                    AccountId = stored.AccountId,
                    TokenHash = hashRefresh,
                    FamilyId = stored.FamilyId,
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays),
                    Revoked = false
                });

                // Look up device name for the new access token claims
                var device = scope.Get<Device>()
                    .Where(x => x.DeviceId == stored.DeviceId)
                    .Select(x => new { x.Description })
                    .FirstOrDefault();

                scope.SaveChanges();

                var accessToken = GenerateAccessToken(stored.AccountId, stored.DeviceId, device?.Description ?? string.Empty);
                return new TokenPair { AccessToken = accessToken, RefreshToken = rawRefresh };
            }
        }

        /// <summary>Generates a new API Key in the mma_ format.</summary>
        public static string GenerateApiKey()
        {
            var bytes = RandomNumberGenerator.GetBytes(16);
            return "mma_" + Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private string GenerateAccessToken(int accountId, int deviceId, string deviceName)
        {
            var claims = new List<Claim>
            {
                new Claim("accountId", accountId.ToString()),
                new Claim("deviceId", deviceId.ToString()),
                new Claim("deviceName", deviceName),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtIssuer,
                audience: _jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(AccessTokenMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static (string raw, string hash) GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            var raw = "rt_" + Convert.ToHexString(bytes).ToLowerInvariant();
            return (raw, Hash(raw));
        }

        private static string Hash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static void RevokeFamily(IDbContextScope scope, Guid familyId)
        {
            var family = scope.Get<DeviceRefreshToken>()
                .Where(x => x.FamilyId == familyId && !x.Revoked)
                .ToList();
            foreach (var t in family)
                t.Revoked = true;
        }
    }
}
