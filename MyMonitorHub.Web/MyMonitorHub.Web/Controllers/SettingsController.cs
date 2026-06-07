using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MyMonitorHub.Domain.Config;
using MyMonitorHub.Domain.Dto;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Exceptions;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Models;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Service;
using MyMonitorHub.Domain.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace MyMonitorHub.Web.Controllers
{
    [Authorize]
    public class SettingsController : BaseController
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;
        private readonly IWebHostEnvironment _env;
        private readonly IMonitorAuthService _authService;

        public SettingsController(IDbContextScopeFactory contextScopeFactory, IWebHostEnvironment env, IMonitorAuthService authService)
            : base(contextScopeFactory)
        {
            _contextScopeFactory = contextScopeFactory;
            _env = env;
            _authService = authService;
        }

        public IActionResult Profile(string? organization)
        {
            ViewBag.Organization = organization;
            SetOwnedOrganizations();
            var userModel = GetModel(Helper.UserId);
            ViewBag.Avatar = GetAvatar(userModel.UserId, userModel.Email);
            UserSecurityCheck(userModel != null, Helper.UserId, UserMode.View);
            ViewBag.success = false;
            ViewBag.UserEmail = userModel!.Email;
            return View();
        }

        public IActionResult AlertMaintenance(int id = -1)
        {
            if (id == -1) id = Helper.UserId;
            if (id != Helper.UserId && !Authorizer.Authorize(Permissions.CanViewAnyone))
                throw new SecurityException(Permissions.CanViewAnyone.FailMessage);
            var data = new UserService(_contextScopeFactory).GetAlertMaintenance(id);
            return View(data);
        }

        public IActionResult Billing(string? organization)
        {
            ViewBag.Organization = organization;
            SetOwnedOrganizations();
            var userModel = GetModel(Helper.UserId);
            ViewBag.Avatar = GetAvatar(userModel.UserId, userModel.Email);
            UserSecurityCheck(userModel != null, Helper.UserId, UserMode.View);
            ViewBag.success = false;
            ViewBag.UserEmail = userModel!.Email;
            return View();
        }

        public IActionResult SecurityHistory(string? organization)
        {
            ViewBag.Organization = organization;
            SetOwnedOrganizations();
            var userModel = GetModel(Helper.UserId);
            ViewBag.Avatar = GetAvatar(userModel.UserId, userModel.Email);
            UserSecurityCheck(userModel != null, Helper.UserId, UserMode.View);
            ViewBag.success = false;
            ViewBag.UserEmail = userModel!.Email;
            return View();
        }

        public IActionResult Applications(string? organization)
        {
            ViewBag.Organization = organization;
            SetOwnedOrganizations();
            var userModel = GetModel(Helper.UserId);
            ViewBag.Avatar = GetAvatar(userModel.UserId, userModel.Email);
            UserSecurityCheck(userModel != null, Helper.UserId, UserMode.View);
            ViewBag.success = false;
            ViewBag.UserEmail = userModel!.Email;
            return View();
        }

        public IActionResult Owners(string? organization)
        {
            ViewBag.Organization = organization;
            SetOwnedOrganizations();
            var userModel = GetModel(Helper.UserId);
            ViewBag.Avatar = GetAvatar(userModel.UserId, userModel.Email);
            UserSecurityCheck(userModel != null, Helper.UserId, UserMode.View);
            ViewBag.success = false;
            ViewBag.UserEmail = userModel!.Email;
            return View();
        }

        public IActionResult Members(string? organization, int? id)
        {
            if (!Authorizer.Authorize(Permissions.CanViewAnyone))
                throw new SecurityException(Permissions.CanViewAnyone.FailMessage);

            ViewBag.Organization = organization;
            SetOwnedOrganizations();
            var userModel = GetModel(Helper.UserId);
            ViewBag.Avatar = GetAvatar(userModel.UserId, userModel.Email);
            UserSecurityCheck(userModel != null, Helper.UserId, UserMode.View);
            ViewBag.success = false;
            ViewBag.UserEmail = userModel!.Email;
            ViewBag.CurrentUserId = Helper.UserId;
            ViewBag.CanEditRoles = Helper.IsOwner || Helper.IsAdministrator;

            var roleService = new RoleService(_contextScopeFactory);
            var systemRoles = roleService.GetSystemRoles();
            var customRoles = roleService.GetCustomRoles(Helper.AccountId);
            var allRoles = systemRoles.Concat(customRoles).ToList();
            ViewBag.AvailableRoles = allRoles;

            var datas = new MemberService(_contextScopeFactory).GetAllForOrganization(Helper.AccountId, id);

            foreach (var data in datas)
            {
                FixAlertTo(data);
                data.Avatar = GetAvatar(data.UserUserId, data.UserEmail);
            }

            return View(datas);
        }

        [HttpPost]
        public IActionResult UpdateMemberRole(int memberId, int roleId)
        {
            if (!Helper.IsOwner && !Helper.IsAdministrator)
                return Json(new { success = false, message = "You do not have permission to change member roles." });

            new MemberService(_contextScopeFactory).UpdateMemberRole(memberId, roleId, Helper.AccountId);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult RemoveMember(int memberId)
        {
            if (!Helper.IsOwner && !Helper.IsAdministrator)
                return Json(new { success = false, message = "You do not have permission to remove members." });

            var removed = new MemberService(_contextScopeFactory).RemoveMember(memberId, Helper.AccountId);
            return Json(new { success = removed });
        }

        [HttpPost]
        public IActionResult Reset2Fa(int userId)
        {
            if (!Helper.IsOwner && !Helper.IsAdministrator)
                return Json(new { success = false, message = "You do not have permission to reset 2FA." });

            if (userId == Helper.UserId)
                return Json(new { success = false, message = "You cannot reset your own 2FA from this page." });

            bool isMember;
            using (var scope = _contextScopeFactory.Create())
            {
                isMember = scope.Get<Member>().Any(x => x.UserId == userId && x.Account.AccountId == Helper.AccountId);
            }

            if (!isMember)
                return Json(new { success = false, message = "User not found in this organization." });

            _authService.Reset2Fa(userId);
            return Json(new { success = true });
        }

        private void FixAlertTo(MemberService.MemberModel data)
        {
            var emails = data.UserEmailNotifications.Where(x => x.Disabled != true).ToList();
            if (emails.Count == 0)
            {
                data.SendAlertsTo = "undefined";
                return;
            }

            data.SendAlertsTo = emails.Count > 1 ? $"Emails({emails.Count})" : emails[0].Email;
        }

        private void SetOwnedOrganizations()
        {
            var service = new OrganizationService(_contextScopeFactory);
            var organizations = service.GetOwnedOrganizations();
            foreach (var organization in organizations)
                organization.AvatarSrc = GetOrgAvatar(organization.AccountAccountId, OutgoingEmailConfig.From);
            ViewBag.Organizations = organizations;
        }

        private string GetOrgAvatar(int accountId, string? email)
        {
            var physicalPath = Path.Combine(_env.WebRootPath, "Images", "Avatars", $"org-{accountId}.png");
            if (System.IO.File.Exists(physicalPath))
                return Url.Content($"~/Images/Avatars/org-{accountId}.png");
            if (string.IsNullOrEmpty(email))
                return GetGravatarSource("default");
            return GetGravatarSource(email.ToLower());
        }

        private string GetGravatarSource(string email)
        {
            var hash = ComputeMd5(email);
            return $"https://www.gravatar.com/avatar/{hash}?s=100&d=retro";
        }

        private static string ComputeMd5(string input)
        {
            var data = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(data).ToLower();
        }

        private string GetAvatar(int userId, string email)
        {
            var physicalPath = Path.Combine(_env.WebRootPath, "Images", "Avatars", $"user-{userId}.png");
            return System.IO.File.Exists(physicalPath)
                ? Url.Content($"~/Images/Avatars/user-{userId}.png")
                : GetGravatarSource(email.ToLower());
        }

        private UserModel? GetModel(int? id)
        {
            id ??= Helper.UserId;
            using var scope = _contextScopeFactory.Create();
            var user = scope.Get<User>().FirstOrDefault(x => x.UserId == id.Value);
            if (user == null) return null;
            return new UserModel
            {
                CellPhone = user.CellPhone,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                HomePhone = user.HomePhone,
                WorkPhone = user.WorkPhone,
                UserId = user.UserId
            };
        }

        private void UserSecurityCheck(bool found, int? id, UserMode mode)
        {
            if (!found) throw new SecurityException("User does not exist");

            ViewBag.saveDisabled = "";
            ViewBag.passwordDisabled = new { @class = "form-control" };
            if (!Helper.IsAdministrator && !Helper.IsOwner && id != Helper.UserId)
                ViewBag.PasswordDisabled = new { @class = "form-control", disabled = "Disabled" };

            if (Helper.IsAdministrator) return;
            if (id == Helper.UserId) return;

            if (!Authorizer.Authorize(Permissions.CanViewAnyone))
                throw new SecurityException(Permissions.CanViewAnyone.FailMessage);

            if (!Authorizer.Authorize(Permissions.CanEditAnyone))
            {
                if (mode == UserMode.View)
                    ViewBag.saveDisabled = "Disabled = 'Disabled'";
                else
                    throw new SecurityException(Permissions.CanEditAnyone.FailMessage);
            }
        }

        private enum UserMode { View, Edit }
    }
}
