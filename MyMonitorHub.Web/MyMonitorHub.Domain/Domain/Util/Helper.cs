using System;
using System.Linq;
using System.Text.Json;
using System.Xml;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyMonitorHub.Domain.Util
{
    /// <summary>
    /// Application-wide session / request helper.
    /// Call <see cref="Configure"/> once at startup (in Program.cs) to wire in the
    /// <see cref="IHttpContextAccessor"/> that allows static access to the current request.
    /// </summary>
    public static class Helper
    {
        private static ILogger _logger = NullLogger.Instance;

        private static IHttpContextAccessor? _httpContextAccessor;

        /// <summary>Physical path to the wwwroot folder. Set from Program.cs via IWebHostEnvironment.</summary>
        public static string WebRootPath { get; private set; } = string.Empty;

        /// <summary>Called from Program.cs after the host is built.</summary>
        public static void Configure(IHttpContextAccessor httpContextAccessor, string webRootPath, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(typeof(Helper).FullName!);
            _httpContextAccessor = httpContextAccessor;
            if (!string.IsNullOrEmpty(webRootPath))
                WebRootPath = webRootPath;
        }

        /// <summary>Returns the base URL of the current request (scheme + host + PathBase), or empty string if no HTTP context.</summary>
        public static string GetBaseUrl()
        {
            var ctx = _httpContextAccessor?.HttpContext;
            if (ctx == null) return string.Empty;
            return $"{ctx.Request.Scheme}://{ctx.Request.Host}{ctx.Request.PathBase}";
        }

        public static ISession Session
        {
            get
            {
                var ctx = _httpContextAccessor?.HttpContext
                    ?? throw new InvalidOperationException("No HttpContext available");
                var session = ctx.Session
                    ?? throw new InvalidOperationException("Session is not available");
                _logger.LogDebug("Session id = {0}", session.Id);
                return session;
            }
        }

        // ── primitive session values ─────────────────────────────────────────────

        public static bool IsBeta
        {
            get
            {
                try { return Session.GetBoolean("role.isbeta") ?? false; }
                catch { return false; }
            }
            set => Session.SetBoolean("role.isbeta", value);
        }

        public static bool IsAdministrator
        {
            get
            {
                try { return Session.GetBoolean("role.isadmin") ?? false; }
                catch { return false; }
            }
            set => Session.SetBoolean("role.isadmin", value);
        }

        public static bool IsOwner
        {
            get
            {
                try { return Session.GetBoolean("role.isowner") ?? false; }
                catch { return false; }
            }
            set => Session.SetBoolean("role.isowner", value);
        }

        public static int AccountId
        {
            get => Session.GetInt32("AccountId") ?? 0;
            set => Session.SetInt32("AccountId", value);
        }

        public static string? Email
        {
            get => Session.GetString("System.user");
            set => Session.SetString("System.user", value ?? string.Empty);
        }

        public static int UserId
        {
            get
            {
                try { return Session.GetInt32("System.userId") ?? -1; }
                catch { return -1; }
            }
            set => Session.SetInt32("System.userId", value);
        }

        // ── complex session values (JSON-serialised) ─────────────────────────────

        public static XmlDocument? RoleDocument
        {
            get
            {
                var xml = Session.GetString("role.document");
                if (string.IsNullOrEmpty(xml)) return null;
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                return doc;
            }
            set
            {
                if (value == null) Session.Remove("role.document");
                else Session.SetString("role.document", value.OuterXml);
            }
        }

        public static OrganizationService.OrganizationInfo? CurrentOrganizationInfo
        {
            get
            {
                var json = Session.GetString("CurrentOrganizationInfo");
                if (string.IsNullOrEmpty(json)) return null;
                return JsonSerializer.Deserialize<OrganizationService.OrganizationInfo>(json);
            }
            set
            {
                if (value == null) Session.Remove("CurrentOrganizationInfo");
                else Session.SetString("CurrentOrganizationInfo",
                    JsonSerializer.Serialize(value));
            }
        }

        // ── setup methods ────────────────────────────────────────────────────────

        public static void Setup(IDbContextScopeFactory contextScopeFactory, string email)
        {
            using var scope = contextScopeFactory.Create();
            var u = scope.Get<User>()
                .Where(x => x.Email == email)
                .Select(x => new { x.Email, x.UserId })
                .FirstOrDefault();

            if (u == null)
                throw new Exception($"User '{email}' is not attached to an account.");

            InitMemberVars();
            Email = u.Email;
            UserId = u.UserId;
            SetupMember(contextScopeFactory);
        }

        private static void InitMemberVars()
        {
            AccountId = 0;
            RoleDocument = null;
            IsAdministrator = false;
            IsOwner = false;
        }

        public static void SetupMember(IDbContextScopeFactory contextScopeFactory)
        {
            InitMemberVars();

            var orgInfo = CurrentOrganizationInfo;
            if (orgInfo == null)
            {
                orgInfo = new OrganizationService(contextScopeFactory).GetDefaultOrganization(UserId);
                if (orgInfo != null) CurrentOrganizationInfo = orgInfo;
            }

            if (orgInfo == null) return;

            AccountId = orgInfo.OrganizationId;

            if (orgInfo.RoleRoleCode == "Administrator")
            {
                IsAdministrator = true;
                IsOwner = true;
            }
            else if (orgInfo.RoleRoleCode == "Owner")
            {
                IsAdministrator = false;
                IsOwner = true;
            }
            else
            {
                IsAdministrator = false;
                IsOwner = false;

                if (orgInfo.RoleRoleXML != null)
                {
                    var doc = new XmlDocument();
                    doc.LoadXml(orgInfo.RoleRoleXML);
                    RoleDocument = doc;
                }

            }
        }

    }

    /// <summary>Extension methods for ISession to handle bool values.</summary>
    internal static class SessionExtensionsHelper
    {
        public static bool? GetBoolean(this ISession session, string key)
        {
            var val = session.GetInt32(key);
            if (val == null) return null;
            return val != 0;
        }

        public static void SetBoolean(this ISession session, string key, bool value)
            => session.SetInt32(key, value ? 1 : 0);

        private static readonly System.Text.Json.JsonSerializerOptions _sessionJsonOptions =
            new System.Text.Json.JsonSerializerOptions { IncludeFields = true };

        public static T? GetObject<T>(this ISession session, string key) where T : class
        {
            var json = session.GetString(key);
            return json == null ? null : System.Text.Json.JsonSerializer.Deserialize<T>(json, _sessionJsonOptions);
        }

        public static void SetObject<T>(this ISession session, string key, T value)
            => session.SetString(key, System.Text.Json.JsonSerializer.Serialize(value, _sessionJsonOptions));
    }
}
