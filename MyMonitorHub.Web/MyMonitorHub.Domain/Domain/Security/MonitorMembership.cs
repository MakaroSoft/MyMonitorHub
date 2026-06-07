using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MyMonitorHub.Domain.Security
{
    /// <summary>
    /// Custom authentication service, replaces the legacy System.Web.Security.MembershipProvider.
    /// Registered as <see cref="IMonitorAuthService"/> in the DI container.
    /// </summary>
    public class MonitorAuthService : IMonitorAuthService
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;
        private readonly ILogger<MonitorAuthService> _logger;
        private readonly IConfiguration _configuration;

        public MonitorAuthService(IDbContextScopeFactory contextScopeFactory, ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _contextScopeFactory = contextScopeFactory;
            _logger = loggerFactory.CreateLogger<MonitorAuthService>();
            _configuration = configuration;
        }

        public bool ValidateUser(string email, string password)
        {
            using var scope = _contextScopeFactory.Create();
            var user = scope.Get<User>().FirstOrDefault(x => x.Email == email);

            if (user == null) return false;

            if (!Util.PasswordHasher.Verify(user.Password, password))
                return false;

            return true;
        }

        public string? GetCanonicalEmail(string login)
        {
            if (string.IsNullOrWhiteSpace(login)) return null;
            using var scope = _contextScopeFactory.Create();
            var user = scope.Get<User>().FirstOrDefault(x => x.Email == login);
            return user?.Email;
        }

        public (string? Token, string? Error) StartRegistration(string email, string password, string firstName, string lastName)
        {
            using var scope = _contextScopeFactory.Create();

            if (scope.Get<User>().Any(x => x.Email == email))
                return (null, "An account with that email address already exists.");

            // Remove any previous unverified attempts for this email
            var existing = scope.Get<PendingRegistration>().Where(x => x.Email == email).ToList();
            foreach (var old in existing)
                scope.Delete(old);

            var token = GenerateToken();
            var now = DateTime.UtcNow;

            var pending = new PendingRegistration
            {
                Email = email,
                Password = Util.PasswordHasher.Hash(password),
                FirstName = firstName,
                LastName = lastName,
                Token = token,
                CreatedAt = now,
                ExpiresAt = now.AddHours(24)
            };

            scope.Add(pending);
            scope.SaveChanges();

            return (token, null);
        }

        public (string? Email, string? Error) ConfirmRegistration(string token)
        {
            string email;
            int newUserId;

            using (var scope = _contextScopeFactory.Create())
            {
                var pending = scope.Get<PendingRegistration>().FirstOrDefault(x => x.Token == token);

                if (pending == null)
                    return (null, "The verification link is invalid or has already been used.");

                if (DateTime.UtcNow > pending.ExpiresAt)
                {
                    scope.Delete(pending);
                    scope.SaveChanges();
                    return (null, "The verification link has expired. Please register again.");
                }

                if (scope.Get<User>().Any(x => x.Email == pending.Email))
                {
                    scope.Delete(pending);
                    scope.SaveChanges();
                    return (null, "An account with that email address already exists.");
                }

                email = pending.Email;

                var user = new User
                {
                    Email = email,
                    Password = pending.Password,
                    FirstName = pending.FirstName,
                    LastName = pending.LastName,
                    TwoFactorEnabled = false
                };

                scope.Add(user);
                scope.Delete(pending);
                scope.SaveChanges();

                newUserId = user.UserId;
            } // first scope fully disposed here; Contexts list is now empty

            var ownerOrgId = _configuration.GetValue<int>("App:OwnerOrganizationId");

            using (var memberScope = _contextScopeFactory.Create()) // now creates a root scope
            {
                var isFirstUser = !memberScope.Get<User>().Any(u => u.UserId != newUserId);

                var roleCode = isFirstUser ? "Owner" : "NoAccess";
                var role = memberScope.Get<Role>()
                    .FirstOrDefault(r => r.RoleCode == roleCode)
                    ?? throw new InvalidOperationException($"Required role '{roleCode}' was not found in the database.");

                memberScope.Add(new Member
                {
                    OrganizationId = ownerOrgId,
                    UserId = newUserId,
                    RoleId = role.RoleId,
                    DefaultOrganization = true
                });
                memberScope.SaveChanges();
            }

            return (email, null);
        }

        public string? StartPasswordReset(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogDebug("StartPasswordReset: empty email supplied");
                return null;
            }

            _logger.LogDebug("StartPasswordReset: looking up email '{Email}'", email);

            using var scope = _contextScopeFactory.Create();

            // Only issue a token when exactly one user matches; ambiguous and missing
            // cases both return null so the caller can return a generic response.
            var users = scope.Get<User>().Where(x => x.Email == email).ToList();
            _logger.LogDebug("StartPasswordReset: found {Count} user(s) matching '{Email}'", users.Count, email);

            if (users.Count == 0)
            {
                // Log stored addresses that differ only by case to help diagnose typos.
                var caseVariants = scope.Get<User>()
                    .Where(x => x.Email.ToLower() == email.ToLower())
                    .Select(x => x.Email)
                    .ToList();
                if (caseVariants.Count > 0)
                    _logger.LogDebug(
                        "StartPasswordReset: no exact match for '{Email}', but found case variant(s): {Variants}",
                        email, string.Join(", ", caseVariants));
                else
                    _logger.LogDebug("StartPasswordReset: '{Email}' does not exist in the database", email);
                return null;
            }

            if (users.Count > 1)
            {
                _logger.LogWarning(
                    "StartPasswordReset: {Count} duplicate accounts found for '{Email}' — token not issued",
                    users.Count, email);
                return null;
            }

            var existing = scope.Get<PendingPasswordReset>().Where(x => x.Email == email).ToList();
            foreach (var old in existing)
                scope.Delete(old);

            var token = GenerateToken();
            var now = DateTime.UtcNow;

            scope.Add(new PendingPasswordReset
            {
                Email = email,
                Token = token,
                CreatedAt = now,
                ExpiresAt = now.AddHours(1)
            });

            scope.SaveChanges();
            _logger.LogDebug("StartPasswordReset: token created for '{Email}', expires at {Expiry:u}", email, now.AddHours(1));
            return token;
        }

        public (string? Email, string? Error) ResetPassword(string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(token))
                return (null, "The reset link is invalid.");
            if (string.IsNullOrWhiteSpace(newPassword))
                return (null, "Password is required.");

            using var scope = _contextScopeFactory.Create();

            var pending = scope.Get<PendingPasswordReset>().FirstOrDefault(x => x.Token == token);
            if (pending == null)
                return (null, "The reset link is invalid or has already been used.");

            if (DateTime.UtcNow > pending.ExpiresAt)
            {
                scope.Delete(pending);
                scope.SaveChanges();
                return (null, "The reset link has expired. Please request a new one.");
            }

            var user = scope.Get<User>().FirstOrDefault(x => x.Email == pending.Email);
            if (user == null)
            {
                scope.Delete(pending);
                scope.SaveChanges();
                return (null, "The account associated with this link no longer exists.");
            }

            user.Password = Util.PasswordHasher.Hash(newPassword);
            scope.Delete(pending);

            // Invalidate any other outstanding reset tokens for this account.
            var otherTokens = scope.Get<PendingPasswordReset>()
                .Where(x => x.Email == pending.Email && x.Token != token)
                .ToList();
            foreach (var other in otherTokens)
                scope.Delete(other);

            scope.SaveChanges();
            return (pending.Email, null);
        }

        public IList<string> GenerateRecoveryCodes(int userId)
        {
            using var scope = _contextScopeFactory.Create();

            var existing = scope.Get<UserRecoveryCode>().Where(x => x.UserId == userId).ToList();
            foreach (var old in existing)
                scope.Delete(old);

            var plainCodes = new List<string>(8);
            for (var i = 0; i < 8; i++)
            {
                var code = GenerateRecoveryCode();
                scope.Add(new UserRecoveryCode
                {
                    UserId = userId,
                    CodeHash = HashRecoveryCode(code)
                });
                plainCodes.Add(code);
            }

            scope.SaveChanges();
            return plainCodes;
        }

        public bool RedeemRecoveryCode(string email, string code)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
                return false;

            using var scope = _contextScopeFactory.Create();

            var user = scope.Get<User>().FirstOrDefault(x => x.Email == email);
            if (user == null) return false;

            var hash = HashRecoveryCode(code);
            var recoveryCode = scope.Get<UserRecoveryCode>()
                .FirstOrDefault(x => x.UserId == user.UserId && x.CodeHash == hash && x.UsedAt == null);

            if (recoveryCode == null) return false;

            recoveryCode.UsedAt = DateTime.UtcNow;
            scope.SaveChanges();
            return true;
        }

        public void Reset2Fa(int userId)
        {
            using var scope = _contextScopeFactory.Create();

            var user = scope.Get<User>().FirstOrDefault(x => x.UserId == userId);
            if (user == null) return;

            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;

            var codes = scope.Get<UserRecoveryCode>().Where(x => x.UserId == userId).ToList();
            foreach (var c in codes)
                scope.Delete(c);

            scope.SaveChanges();
        }

        private static string GenerateToken()
        {
            var bytes = new byte[48];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        private static string GenerateRecoveryCode()
        {
            // Excludes visually ambiguous characters: O, 0, I, 1
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var sb = new StringBuilder(9);
            for (var i = 0; i < 8; i++)
            {
                if (i == 4) sb.Append('-');
                sb.Append(chars[RandomNumberGenerator.GetInt32(chars.Length)]);
            }
            return sb.ToString();
        }

        private static string HashRecoveryCode(string code)
        {
            var normalized = code.Trim().ToUpperInvariant().Replace("-", "");
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
