using System.Collections.Generic;
using System.Linq;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Service;
using MyMonitorHub.Domain.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MyMonitorHub.Domain.Tests
{
    public class AgentTokenServiceTests
    {
        private const int AccountId = 1001;
        private const int DeviceId = 42;
        private const string ApiKey = "mma_0123456789abcdef0123456789abcdef";

        private readonly InMemoryDbContextScopeFactory _scopes;
        private readonly AgentTokenService _service;

        public AgentTokenServiceTests()
        {
            _scopes = new InMemoryDbContextScopeFactory();
            SeedDevice(AccountId, DeviceId, ApiKey, "Test Device");

            var config = new TestConfiguration(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-must-be-at-least-32-bytes-long-aaaa",
                ["Jwt:Issuer"] = "mymonitorhub-tests"
            });

            _service = new AgentTokenService(_scopes, NullLoggerFactory.Instance, config);
        }

        [Fact]
        public void IssueTokens_WithValidApiKey_ReturnsPairAndStoresOneUnrevokedRow()
        {
            var pair = _service.IssueTokens(AccountId, DeviceId, ApiKey);

            Assert.NotNull(pair);
            Assert.NotEmpty(pair.AccessToken);
            Assert.StartsWith("rt_", pair.RefreshToken);

            var rows = _scopes.Set<DeviceRefreshToken>();
            Assert.Single(rows);

            var row = rows[0];
            Assert.Equal(DeviceId, row.DeviceId);
            Assert.Equal(AccountId, row.AccountId);
            Assert.False(row.Revoked);
            Assert.Equal(64, row.TokenHash.Length);
            Assert.DoesNotContain(pair.RefreshToken, row.TokenHash);
            Assert.True(row.ExpiresAt > row.IssuedAt);
        }

        [Fact]
        public void IssueTokens_WithInvalidApiKey_ReturnsNullAndStoresNothing()
        {
            var pair = _service.IssueTokens(AccountId, DeviceId, "mma_wrong-key");

            Assert.Null(pair);
            Assert.Empty(_scopes.Set<DeviceRefreshToken>());
        }

        [Fact]
        public void RotateRefreshToken_ValidToken_RevokesOldAndIssuesNewInSameFamily()
        {
            var pair1 = _service.IssueTokens(AccountId, DeviceId, ApiKey);
            Assert.NotNull(pair1);

            var pair2 = _service.RotateRefreshToken(pair1.RefreshToken);

            Assert.NotNull(pair2);
            Assert.NotEqual(pair1.RefreshToken, pair2.RefreshToken);
            Assert.NotEmpty(pair2.AccessToken);

            var rows = _scopes.Set<DeviceRefreshToken>();
            Assert.Equal(2, rows.Count);

            var oldRow = rows[0];
            var newRow = rows[1];

            Assert.True(oldRow.Revoked);
            Assert.False(newRow.Revoked);
            Assert.Equal(oldRow.FamilyId, newRow.FamilyId);
        }

        [Fact]
        public void RotateRefreshToken_ReplayedToken_ReturnsNullAndRevokesEntireFamily()
        {
            var pair1 = _service.IssueTokens(AccountId, DeviceId, ApiKey);
            var pair2 = _service.RotateRefreshToken(pair1!.RefreshToken);
            Assert.NotNull(pair2);

            // Replay the now-revoked pair1 token — triggers theft-detection.
            var replay = _service.RotateRefreshToken(pair1.RefreshToken);
            Assert.Null(replay);

            var rows = _scopes.Set<DeviceRefreshToken>();
            Assert.Equal(2, rows.Count);
            Assert.True(rows.All(r => r.Revoked));

            // The previously-valid pair2 token must also be rejected once the family is revoked.
            var afterRevoke = _service.RotateRefreshToken(pair2.RefreshToken);
            Assert.Null(afterRevoke);
        }

        private void SeedDevice(int accountId, int deviceId, string apiKey, string description)
        {
            using var scope = _scopes.Create();
            scope.Add(new Device
            {
                DeviceId = deviceId,
                AccountId = accountId,
                ApiKey = apiKey,
                Description = description,
                Deleted = false,
            });
            scope.SaveChanges();
        }
    }
}
