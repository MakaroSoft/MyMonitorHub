using System;
using MyMonitorHub.Domain.Exceptions;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Service;
using MyMonitorHub.Domain.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

namespace MyMonitorHub.Web.Controllers
{
        [Authorize]
    public class EmailNotificationController : BaseController
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;
        private readonly ILoggerFactory _loggerFactory;

        public EmailNotificationController(IDbContextScopeFactory contextScopeFactory, ILoggerFactory loggerFactory)
            : base(contextScopeFactory)
        {
            _loggerFactory = loggerFactory;
            _contextScopeFactory = contextScopeFactory;
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                var emailToDelete = new EmailNotificationService(_contextScopeFactory, _loggerFactory).GetDetail(id);
                if (emailToDelete == null)
                    return Json(new { Status = "Fail", Reason = "Email not found" });

                if (emailToDelete.UserId != Helper.UserId && !Authorizer.Authorize(Permissions.CanDeleteAnyone))
                    throw new SecurityException(Permissions.CanDeleteAnyone.FailMessage);
            }
            catch (Exception e)
            {
                return Json(new { Status = "Fail", Reason = e.Message });
            }

            try
            {
                new EmailNotificationService(_contextScopeFactory, _loggerFactory).Delete(id);
                return Json(new { Status = "Success", Response = "Successfully deleted the email", EmailNotificationId = id });
            }
            catch (Exception e)
            {
                return Json(new { Status = "Fail", Reason = e.Message });
            }
        }

        public IActionResult Edit(int id = -1, int userId = -1)
        {
            var data = new EmailNotificationService(_contextScopeFactory, _loggerFactory).GetDetail(id);
            if (data == null) throw new SecurityException("Email not found");

            if (data.EmailNotificationId == -1)
                data.UserId = userId == -1 ? Helper.UserId : userId;

            if (data.UserId != Helper.UserId && !Authorizer.Authorize(Permissions.CanEditAnyone))
                throw new SecurityException(Permissions.CanEditAnyone.FailMessage);

            return View(data);
        }

        [HttpPost]
        public JsonResult Edit([FromBody] EmailNotificationService.EmailNotificationModel model)
        {
            try
            {
                if (model.EmailNotificationId == -1)
                {
                    if (model.UserId != Helper.UserId && !Authorizer.Authorize(Permissions.CanEditAnyone))
                        throw new SecurityException(Permissions.CanEditAnyone.FailMessage);

                    var emailNotificationId = new EmailNotificationService(_contextScopeFactory, _loggerFactory).Insert(model);
                    return Json(new { Status = "Success", Response = "Successfully Created Email Entry", EmailNotificationId = emailNotificationId });
                }
                else
                {
                    var emailToUpdate = new EmailNotificationService(_contextScopeFactory, _loggerFactory).GetDetail(model.EmailNotificationId);
                    if (emailToUpdate == null)
                        return Json(new { Status = "Fail", Reason = "Email not found" });

                    if (emailToUpdate.UserId != Helper.UserId && !Authorizer.Authorize(Permissions.CanEditAnyone))
                        throw new SecurityException(Permissions.CanEditAnyone.FailMessage);

                    new EmailNotificationService(_contextScopeFactory, _loggerFactory).Update(model);
                    return Json(new { Status = "Success", Response = "Successfully Updated Email Entry" });
                }
            }
            catch (Exception e)
            {
                return Json(new { Status = "Fail", Email = model.Email, Reason = e.Message });
            }
        }
    }
}
