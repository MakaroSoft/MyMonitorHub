using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Models;
using MyMonitorHub.Domain.Service;
using MyMonitorHub.Domain.Util;
using MyMonitorHub.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using QRCoder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Emailer = MyMonitorHub.Domain.Service.Emailer;

namespace MyMonitorHub.Web.Controllers
{
    public class AccountController : BaseController
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;
        private readonly IMonitorAuthService _authService;
        private readonly IDataProtector _protector;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<AccountController> _logger;
        private readonly LoginFailureTracker _loginFailureTracker;
        private readonly IConfiguration _configuration;

        private const string RememberDeviceCookieName = "MSM_RememberDevice";
        private const string RememberDevicePurpose = "MyMonitorHub.RememberDevice";

        public AccountController(
            IDbContextScopeFactory contextScopeFactory,
            IMonitorAuthService authService,
            IDataProtectionProvider dataProtectionProvider,
            ICompositeViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            ILoggerFactory loggerFactory,
            LoginFailureTracker loginFailureTracker,
            IConfiguration configuration)
            : base(contextScopeFactory, viewEngine, tempDataProvider)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<AccountController>();
            _contextScopeFactory = contextScopeFactory;
            _configuration = configuration;
            _authService = authService;
            _protector = dataProtectionProvider.CreateProtector(RememberDevicePurpose);
            _loginFailureTracker = loginFailureTracker;
        }

        private void SetRememberDeviceCookie(string email, TimeSpan duration)
        {
            var expires = DateTime.UtcNow.Add(duration);
            var payload = Encoding.UTF8.GetBytes(email + "|" + expires.Ticks);
            var protectedBytes = _protector.Protect(payload);
            Response.Cookies.Append(RememberDeviceCookieName, Convert.ToBase64String(protectedBytes),
                new Microsoft.AspNetCore.Http.CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = expires,
                    Path = "/",
                    SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax
                });
        }

        private bool IsRememberDeviceCookieValid(string email)
        {
            if (!Request.Cookies.TryGetValue(RememberDeviceCookieName, out var cookieValue)
                || string.IsNullOrWhiteSpace(cookieValue))
                return false;
            try
            {
                var bytes = Convert.FromBase64String(cookieValue);
                var unprotected = _protector.Unprotect(bytes);
                var text = Encoding.UTF8.GetString(unprotected);
                var parts = text.Split('|');
                if (parts.Length != 2) return false;
                if (!string.Equals(parts[0], email, StringComparison.Ordinal)) return false;
                if (!long.TryParse(parts[1], out var ticks)) return false;
                var expiry = new DateTime(ticks, DateTimeKind.Utc);
                return DateTime.UtcNow < expiry;
            }
            catch { return false; }
        }

        private async Task SignInUser(string email, bool persistent)
        {
            var claims = new[] { new Claim(ClaimTypes.Name, email) };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = persistent });
        }

        // GET: /Account/RecoverPassword
        [ResponseCache(NoStore = true, Duration = 0, Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None)]
        public IActionResult RecoverPassword()
        {
            ViewBag.success = false;
            return View();
        }

        // POST: /Account/RecoverPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(AgentAuthRateLimitPolicies.AccountByIp)]
        [ResponseCache(NoStore = true, Duration = 0, Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None)]
        public async Task<IActionResult> RecoverPassword(RecoverPasswordModel model)
        {
            ViewBag.success = false;
            if (!ModelState.IsValid) return View();

            // Always present a generic success response to avoid revealing whether the
            // email exists. The token is only created and emailed when there is exactly
            // one matching user account.
            var token = _authService.StartPasswordReset(model.Email);

            if (token != null)
            {
                string fullName;
                using (var scope = _contextScopeFactory.Create())
                {
                    var user = scope.Get<User>().FirstOrDefault(x => x.Email == model.Email);
                    fullName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : model.Email;
                }

                var resetUrl = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme);
                var logoUrl = $"{Request.Scheme}://{Request.Host}/Content/ms/images/Logo.png";

                ViewBag.imageUrl = logoUrl;
                ViewBag.link = resetUrl;
                ViewBag.fullName = fullName;

                var html = await RenderRazorViewToString("~/Views/Email/PasswordResetLink.cshtml", null);
                try
                {
                    new Emailer(_contextScopeFactory, _loggerFactory).SendDirectHTML(
                        model.Email, "Reset your MyMonitorHub password", html);
                    _loggerFactory.CreateLogger<AccountController>()
                        .LogDebug("RecoverPassword: password-reset email sent to '{Email}'", model.Email);
                }
                catch (Exception ex)
                {
                    _loggerFactory.CreateLogger<AccountController>()
                        .LogError(ex, "RecoverPassword: failed to send password-reset email to '{Email}'", model.Email);
                }
            }

            ViewBag.success = true;
            return View();
        }

        // GET: /Account/ResetPassword
        [ResponseCache(NoStore = true, Duration = 0, Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None)]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Error = "The reset link is invalid.";
                return View(new ResetPasswordModel { Token = string.Empty });
            }

            return View(new ResetPasswordModel { Token = token });
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(AgentAuthRateLimitPolicies.AccountByIp)]
        [ResponseCache(NoStore = true, Duration = 0, Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None)]
        public IActionResult ResetPassword(ResetPasswordModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var (email, error) = _authService.ResetPassword(model.Token, model.Password);
            if (error != null)
            {
                ViewBag.Error = error;
                return View(model);
            }

            TempData["Message"] = "Your password has been reset. You can now sign in.";
            return RedirectToAction("LogOn");
        }

        // GET: /Account/Register
        public IActionResult Register() => View();

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(AgentAuthRateLimitPolicies.AccountByIp)]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var email = model.Email.Trim();
            var (token, error) = _authService.StartRegistration(
                email,
                model.Password,
                model.FirstName.Trim(),
                model.LastName.Trim());

            if (error != null)
            {
                ModelState.AddModelError("", error);
                return View(model);
            }

            var verifyUrl = Url.Action("VerifyEmail", "Account", new { token }, Request.Scheme);
            var logoUrl = $"{Request.Scheme}://{Request.Host}/Content/ms/images/Logo.png";
            ViewBag.imageUrl = logoUrl;
            ViewBag.link = verifyUrl;
            ViewBag.fullName = model.FirstName.Trim() + " " + model.LastName.Trim();

            var html = await RenderRazorViewToString("~/Views/Email/VerifyEmail.cshtml", null);
            new Emailer(_contextScopeFactory, _loggerFactory).SendDirectHTML(
                email, "Verify your MyMonitorHub account", html);

            return RedirectToAction("RegistrationPending");
        }

        // GET: /Account/RegistrationPending
        public IActionResult RegistrationPending() => View();

        // GET: /Account/VerifyEmail
        public async Task<IActionResult> VerifyEmail(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Register");

            var (email, error) = _authService.ConfirmRegistration(token);

            if (error != null)
            {
                ViewBag.Error = error;
                return View();
            }

            await NotifyOwnersOfNewRegistration(email!);

            HttpContext.Session.SetObject("LogOnModel", new LogOnModel
            {
                Email = email!,
                Password = string.Empty,
                RememberMe = false
            });

            return RedirectToAction("EnableAuthenticator");
        }

        private async Task NotifyOwnersOfNewRegistration(string newUserEmail)
        {
            try
            {
                string newUserFullName;
                List<string> ownerEmails;

                using (var scope = _contextScopeFactory.Create())
                {
                    var newUser = scope.Get<User>().FirstOrDefault(x => x.Email == newUserEmail);
                    newUserFullName = newUser != null
                        ? $"{newUser.FirstName} {newUser.LastName}".Trim()
                        : newUserEmail;

                    var ownerOrgId = _configuration.GetValue<int>("App:OwnerOrganizationId");
                    ownerEmails = scope.Get<Member>()
                        .Where(x => x.OrganizationId == ownerOrgId && x.Role.RoleCode == "Owner")
                        .Select(x => x.User.Email)
                        .ToList();
                }

                if (ownerEmails.Count == 0) return;

                var logoUrl = $"{Request.Scheme}://{Request.Host}/Content/ms/images/Logo.png";
                var membersUrl = Url.Action("Members", "Settings", null, Request.Scheme);

                ViewBag.newUserFullName = newUserFullName;
                ViewBag.newUserEmail = newUserEmail;
                ViewBag.membersUrl = membersUrl;
                ViewBag.imageUrl = logoUrl;

                var html = await RenderRazorViewToString("~/Views/Email/NewUserRegistered.cshtml", null);
                var emailer = new Emailer(_contextScopeFactory, _loggerFactory);

                foreach (var ownerEmail in ownerEmails)
                {
                    emailer.SendDirectHTML(ownerEmail,
                        $"New User Registered: {newUserFullName}", html);
                }
            }
            catch (Exception ex)
            {
                _loggerFactory.CreateLogger<AccountController>()
                    .LogError(ex, "Failed to notify owners of new registration for {Email}", newUserEmail);
            }
        }

        // GET: /Account/LogOn
        public IActionResult LogOn() => View();

        // GET: /Account/Get2FA
        public async Task<IActionResult> Get2Fa()
        {
            var model = HttpContext.Session.GetObject<LogOnModel>("LogOnModel");
            if (model == null) return RedirectToAction("LogOn");

            if (IsRememberDeviceCookieValid(model.Email))
            {
                await SignInUser(model.Email, model.RememberMe);
                Helper.Setup(_contextScopeFactory, model.Email);
                var returnUrl = HttpContext.Session.GetString("returnUrl");
                HttpContext.Session.Remove("returnUrl");
                HttpContext.Session.Remove("LogOnModel");
                if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl!);
                var homePage = new UserService(_contextScopeFactory).GetHomePage();
                return Redirect(Url.Content($"~/{homePage}"));
            }

            using (var scope = _contextScopeFactory.Create())
            {
                var user = scope.Get<User>().FirstOrDefault(x => x.Email == model.Email);
                if (user?.TwoFactorEnabled == true && !string.IsNullOrWhiteSpace(user.TwoFactorSecret))
                {
                    ViewBag.UsingAuthenticator = true;
                    return View();
                }
            }

            return RedirectToAction("EnableAuthenticator");
        }

        // POST: /Account/Get2Fa
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(AgentAuthRateLimitPolicies.LoginByIp)]
        public async Task<IActionResult> Get2Fa(InputModel model)
        {
            var returnUrl = HttpContext.Session.GetString("returnUrl");
            var model2 = HttpContext.Session.GetObject<LogOnModel>("LogOnModel");
            if (model2 == null) return RedirectToAction("LogOn");

            var emailKey = LoginFailureTracker.NormalizeEmail(model2.Email);
            if (_loginFailureTracker.IsLockedOut(emailKey))
            {
                _logger.LogWarning("Get2Fa blocked by lockout. IP={Ip} Email={Email}", ClientIp, emailKey);
                ModelState.AddModelError("", "Too many failed sign-in attempts. Please try again in 15 minutes.");
                ViewBag.UsingAuthenticator = true;
                return View(model);
            }

            using (var scope = _contextScopeFactory.Create())
            {
                var user = scope.Get<User>().FirstOrDefault(x => x.Email == model2.Email);
                if (user == null || user.TwoFactorEnabled != true || string.IsNullOrWhiteSpace(user.TwoFactorSecret))
                    return RedirectToAction("EnableAuthenticator");

                var ok = Totp.VerifyCode(user.TwoFactorSecret, model.TwoFactorCode, DateTime.UtcNow, 1);
                if (!ok)
                {
                    _loginFailureTracker.RecordFailure(emailKey);
                    _logger.LogWarning("Get2Fa failed. IP={Ip} Email={Email}", ClientIp, emailKey);
                    ModelState.AddModelError("", "The authenticator code is invalid.");
                    ViewBag.UsingAuthenticator = true;
                    return View(model);
                }
            }

            _loginFailureTracker.ClearFailures(emailKey);

            if (model.RememberMachine)
                SetRememberDeviceCookie(model2.Email, TimeSpan.FromDays(180));

            HttpContext.Session.Remove("returnUrl");
            HttpContext.Session.Remove("LogOnModel");
            await SignInUser(model2.Email, model2.RememberMe);
            Helper.Setup(_contextScopeFactory, model2.Email);

            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl!);
            var home = new UserService(_contextScopeFactory).GetHomePage();
            return Redirect(Url.Content($"~/{home}"));
        }

        // POST: /Account/LogOn
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(AgentAuthRateLimitPolicies.LoginByIp)]
        public async Task<IActionResult> LogOn(LogOnModel model, string? returnUrl)
        {
            if (!ModelState.IsValid) return View(model);

            var emailKey = LoginFailureTracker.NormalizeEmail(model.Email);
            if (_loginFailureTracker.IsLockedOut(emailKey))
            {
                _logger.LogWarning("LogOn blocked by lockout. IP={Ip} Email={Email}", ClientIp, emailKey);
                ModelState.AddModelError("", "Too many failed sign-in attempts. Please try again in 15 minutes.");
                return View(model);
            }

            if (!_authService.ValidateUser(model.Email, model.Password))
            {
                _loginFailureTracker.RecordFailure(emailKey);
                _logger.LogWarning("LogOn failed. IP={Ip} Email={Email}", ClientIp, emailKey);
                ModelState.AddModelError("", "The email address or password provided is incorrect.");
                return View(model);
            }

            _loginFailureTracker.ClearFailures(emailKey);

            var canonicalEmail = _authService.GetCanonicalEmail(model.Email) ?? model.Email;
            model.Email = canonicalEmail;

            if (IsRememberDeviceCookieValid(model.Email))
            {
                await SignInUser(model.Email, model.RememberMe);
                Helper.Setup(_contextScopeFactory, model.Email);
                if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl!);
                var home = new UserService(_contextScopeFactory).GetHomePage();
                return Redirect(Url.Content($"~/{home}"));
            }

            HttpContext.Session.SetString("returnUrl", returnUrl ?? string.Empty);
            HttpContext.Session.SetObject("LogOnModel", model);
            return RedirectToAction("Get2Fa");
        }

        // GET: /Account/LogOff
        public async Task<IActionResult> LogOff()
        {
            var isBeta = Helper.IsBeta;
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Expire any legacy domain-scoped cookie
            Response.Cookies.Append(
                ".ASPXAUTH", "",
                new Microsoft.AspNetCore.Http.CookieOptions
                {
                    Domain = ".myhub.com",
                    Expires = DateTimeOffset.Now.AddYears(-1),
                    HttpOnly = true,
                    Path = "/"
                });

            HttpContext.Session.Clear();

            if (isBeta) Helper.IsBeta = true;

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/EnableAuthenticator
        public IActionResult EnableAuthenticator()
        {
            var email = GetCurrentOrPreAuthEmail();
            if (email == null) return RedirectToAction("LogOn");

            using var scope = _contextScopeFactory.Create();
            var user = scope.Get<User>().FirstOrDefault(x => x.Email == email);
            if (user == null) return RedirectToAction("LogOn");

            if (user.TwoFactorEnabled == true && !string.IsNullOrWhiteSpace(user.TwoFactorSecret))
            {
                TempData["Message"] = "Authenticator is already enabled.";
                return RedirectToAction("Get2Fa");
            }

            var random = new byte[20];
            System.Security.Cryptography.RandomNumberGenerator.Fill(random);
            var secret = Base32.Encode(random);
            HttpContext.Session.SetString("PendingTotpSecret", secret);
            ViewBag.Secret = secret;

            var issuer = "MyMonitorHub";
            var label = Uri.EscapeDataString(email);
            var otpAuth =
                $"otpauth://totp/{issuer}:{label}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&digits=6&period=30&algorithm=SHA1";

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(otpAuth, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            var pngBytes = qrCode.GetGraphic(4);
            ViewBag.QrCodeDataUri = "data:image/png;base64," + Convert.ToBase64String(pngBytes);

            return View();
        }

        // POST: /Account/EnableAuthenticator
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EnableAuthenticator(string code)
        {
            var email = GetCurrentOrPreAuthEmail();
            if (email == null) return RedirectToAction("LogOn");

            var secret = HttpContext.Session.GetString("PendingTotpSecret");
            if (string.IsNullOrWhiteSpace(secret))
            {
                TempData["Message"] = "Setup session expired. Please start again.";
                return RedirectToAction("EnableAuthenticator");
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                ModelState.AddModelError("", "Code is required.");
                ViewBag.Secret = secret;
                return View();
            }

            if (!Totp.VerifyCode(secret, code, DateTime.UtcNow, 1))
            {
                ModelState.AddModelError("", "The code is invalid.");
                ViewBag.Secret = secret;
                return View();
            }

            HttpContext.Session.Remove("PendingTotpSecret");

            int userId = 0;
            using (var scope = _contextScopeFactory.Create())
            {
                var user = scope.Get<User>().FirstOrDefault(x => x.Email == email);
                if (user != null)
                {
                    user.TwoFactorSecret = secret.Trim().Replace(" ", string.Empty);
                    user.TwoFactorEnabled = true;
                    scope.SaveChanges();
                    userId = user.UserId;
                }
            }

            if (userId > 0)
            {
                var plainCodes = _authService.GenerateRecoveryCodes(userId);
                TempData["RecoveryCodes"] = string.Join("\n", plainCodes);
            }

            return RedirectToAction("ShowRecoveryCodes");
        }

        // GET: /Account/ShowRecoveryCodes
        [ResponseCache(NoStore = true, Duration = 0, Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None)]
        public IActionResult ShowRecoveryCodes()
        {
            var codesRaw = TempData["RecoveryCodes"] as string;
            if (string.IsNullOrEmpty(codesRaw))
                return RedirectToAction("Index", "Home");

            ViewBag.Codes = codesRaw.Split('\n')
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToList();

            var preAuth = HttpContext.Session.GetObject<LogOnModel>("LogOnModel");
            ViewBag.NextUrl = preAuth != null
                ? Url.Action("Get2Fa", "Account")
                : (User.Identity?.IsAuthenticated == true
                    ? Url.Action("Index", "Home")
                    : Url.Action("LogOn", "Account"));

            return View();
        }

        // GET: /Account/UseRecoveryCode
        [ResponseCache(NoStore = true, Duration = 0, Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None)]
        public IActionResult UseRecoveryCode()
        {
            var model = HttpContext.Session.GetObject<LogOnModel>("LogOnModel");
            if (model == null) return RedirectToAction("LogOn");
            return View();
        }

        // POST: /Account/UseRecoveryCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(AgentAuthRateLimitPolicies.LoginByIp)]
        [ResponseCache(NoStore = true, Duration = 0, Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None)]
        public async Task<IActionResult> UseRecoveryCode(string recoveryCode)
        {
            var model2 = HttpContext.Session.GetObject<LogOnModel>("LogOnModel");
            if (model2 == null) return RedirectToAction("LogOn");

            var emailKey = LoginFailureTracker.NormalizeEmail(model2.Email);
            if (_loginFailureTracker.IsLockedOut(emailKey))
            {
                _logger.LogWarning("UseRecoveryCode blocked by lockout. IP={Ip} Email={Email}", ClientIp, emailKey);
                ModelState.AddModelError("", "Too many failed sign-in attempts. Please try again in 15 minutes.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(recoveryCode))
            {
                ModelState.AddModelError("", "Recovery code is required.");
                return View();
            }

            var ok = _authService.RedeemRecoveryCode(model2.Email, recoveryCode);
            if (!ok)
            {
                _loginFailureTracker.RecordFailure(emailKey);
                _logger.LogWarning("UseRecoveryCode failed. IP={Ip} Email={Email}", ClientIp, emailKey);
                ModelState.AddModelError("", "The recovery code is invalid or has already been used.");
                return View();
            }

            _loginFailureTracker.ClearFailures(emailKey);
            var returnUrl = HttpContext.Session.GetString("returnUrl");
            HttpContext.Session.Remove("returnUrl");
            HttpContext.Session.Remove("LogOnModel");
            await SignInUser(model2.Email, model2.RememberMe);
            Helper.Setup(_contextScopeFactory, model2.Email);

            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl!);
            var home = new UserService(_contextScopeFactory).GetHomePage();
            return Redirect(Url.Content($"~/{home}"));
        }

        private string? GetCurrentOrPreAuthEmail()
        {
            var preAuth = HttpContext.Session.GetObject<LogOnModel>("LogOnModel");
            if (preAuth != null) return preAuth.Email;
            if (User.Identity?.IsAuthenticated == true) return User.Identity.Name;
            return null;
        }

        private string ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
