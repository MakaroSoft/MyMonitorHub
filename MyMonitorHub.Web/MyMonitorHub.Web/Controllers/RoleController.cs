using System;
using System.Linq;
using MyMonitorHub.Domain.Exceptions;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Service;
using MyMonitorHub.Domain.Util;
using MyMonitorHub.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyMonitorHub.Web.Controllers
{
        [Authorize]
    public class RoleController : BaseController
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;

        public RoleController(IDbContextScopeFactory contextScopeFactory)
            : base(contextScopeFactory)
        {
            _contextScopeFactory = contextScopeFactory;
        }

        public IActionResult Roles()
        {
            if (!Authorizer.Authorize(Permissions.CanViewRoles))
                throw new SecurityException(Permissions.CanViewRoles.FailMessage);
            var svc = new RoleService(_contextScopeFactory);
            var model = new RolesModel
            {
                SystemRoles = svc.GetSystemRoles(),
                CustomRoles = svc.GetCustomRoles(Helper.AccountId)
            };
            ViewBag.EnableAdd = Authorizer.Authorize(Permissions.CanCreateNewRole) ? "" : null;
            ViewBag.EnableDelete = Authorizer.Authorize(Permissions.CanDeleteRoles) ? "" : null;
            return View(model);
        }

        public IActionResult Role(int id)
        {
            if (!Authorizer.Authorize(Permissions.CanViewRoles))
                throw new SecurityException(Permissions.CanViewRoles.FailMessage);
            var data = new RoleService(_contextScopeFactory).Get(id);
            var model = new RoleMaintModel
            {
                Layout = Permissions.GetLayout(),
                Data = data
            };
            ViewBag.EnableEdit = Authorizer.Authorize(Permissions.CanEditRoles) ? "" : "disabled";
            return View(model);
        }

        public IActionResult Insert()
        {
            if (!Authorizer.Authorize(Permissions.CanCreateNewRole))
                throw new SecurityException(Permissions.CanCreateNewRole.FailMessage);
            var model = new RoleMaintModel
            {
                Layout = Permissions.GetLayout(),
                Data = new RoleService.RoleMaintData { Permissions = Permissions.GetEmptyData() }
            };
            return View(model);
        }

        [HttpPost]
        public JsonResult Save(int id, RoleService.RoleMaintData model)
        {
            if (!Authorizer.Authorize(Permissions.CanEditRoles))
                throw new SecurityException(Permissions.CanEditRoles.FailMessage);
            try
            {
                if (id == 0)
                {
                    var roleId = new RoleService(_contextScopeFactory).Insert(model);
                    return Json(new { Status = "Success", Response = "Successfully Created Role", RoleId = roleId });
                }
                else
                {
                    new RoleService(_contextScopeFactory).Update(id, model);
                    return Json(new { Status = "Success", Response = "Successfully Updated Role" });
                }
            }
            catch (Exception e) { return Json(new { Status = "Fail", RoleName = model.Name, Reason = e.Message }); }
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            if (!Authorizer.Authorize(Permissions.CanDeleteRoles))
                throw new SecurityException(Permissions.CanDeleteRoles.FailMessage);
            try
            {
                new RoleService(_contextScopeFactory).Delete(id);
                return Json(new { Status = "Success", Response = "Successfully deleted Role", RoleId = id });
            }
            catch (Exception e) { return Json(new { Status = "Fail", Reason = e.Message }); }
        }
    }
}
