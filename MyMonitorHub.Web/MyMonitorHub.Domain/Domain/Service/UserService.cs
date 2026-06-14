using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MyMonitorHub.Domain.Util;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;

namespace MyMonitorHub.Domain.Service
{
    public class UserService
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;
        public UserService(IDbContextScopeFactory contextScopeFactory)
        {
            _contextScopeFactory = contextScopeFactory;
        }


        public string GetHomePage()
        {
            using (var scope = _contextScopeFactory.Create())
            {
                return
                    scope.Get<User>().Where(x => x.Email == Helper.Email).Select(x => x.HomePage).FirstOrDefault();
            }

        }

        public List<TextValuePair> GetAssignedTosAsPairs()
        {
            using (var scope = _contextScopeFactory.Create())
            {
                if (!Helper.IsAdministrator)
                {
                    return scope.Get<Member>().Where(x => x.OrganizationId == Helper.AccountId)
                        .Select(x => new { email = x.User.Email, userId = x.UserId })
                        .AsEnumerable()
                        .Select(x => new TextValuePair {Text = x.email, Value = x.userId.ToString(CultureInfo.InvariantCulture)})
                        .ToList();
                }
                return scope.Get<Member>()
                    .Select(x => new {
                        description = x.Account.Description,
                        email = x.User.Email,
                        userId = x.UserId })
                    .AsEnumerable()
                    .Select(
                        x => new TextValuePair {Text = x.description + "> " + x.email, Value = x.userId.ToString(CultureInfo.InvariantCulture)})
                    .ToList();
            } // using
        }

        public int GetUserIdByEmail(string email)
        {
            using (var scope = _contextScopeFactory.Create())
            {
                var emailLower = email.ToLower();
                return scope.Get<User>().Where(x => x.Email.ToLower() == emailLower).Select(x => x.UserId).FirstOrDefault();
            }
        }

        public List<string> GetAllEmails()
        {
            using (var scope = _contextScopeFactory.Create())
            {
                return scope.Get<User>().Select(x => x.Email).ToList();
            }
        }

        public AlertMaintenanceModel GetAlertMaintenance(int userId)
        {
            var result = new AlertMaintenanceModel();
            result.UserId = userId;
            using (var scope = _contextScopeFactory.Create())
            {
                result.Email = scope.Get<User>().Where(x => x.UserId == userId).Select(x => x.Email).FirstOrDefault();
                result.Emails = scope.Get<EmailNotification>().Where(x => x.UserId == userId).ProjectToAlertMaintenanceEmailModel().ToList();
            }
            return result;
        }
    } // class

    public class AlertMaintenanceModel
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public List<AlertMaintenanceEmailModel> Emails { get; set; }
    }

    public class AlertMaintenanceEmailModel
    {
        public int EmailNotificationId { get; set; }
        public string Email { get; set; }
        public bool? Disabled { get; set; }
    }
}