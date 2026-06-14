using System;
using System.Collections.Generic;
using System.Linq;
using MyMonitorHub.Domain.Util;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Util;
using Microsoft.Extensions.Logging;

namespace MyMonitorHub.Domain.Service
{
    public class EmailNotificationService
    {
        private readonly ILogger _logger;

        private readonly IDbContextScopeFactory _contextScopeFactory;

        public EmailNotificationService(IDbContextScopeFactory contextScopeFactory, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<EmailNotificationService>();
            _contextScopeFactory = contextScopeFactory;
        }

        public class EmailNotificationModel
        {
            public int EmailNotificationId { get; set; }
            public int UserId { get; set; }
            public string Email { get; set; }
            public bool? Disabled { get; set; }
        }

        public EmailNotificationModel GetDetail(int emailNotificationId)
        {
            if (emailNotificationId == -1)
            {
                return new EmailNotificationModel
                {
                    EmailNotificationId = -1,
                    Email = ""
                };

            }
            using (var scope = _contextScopeFactory.Create())
            {
                return scope.Get<EmailNotification>().Where(x => x.EmailNotificationId == emailNotificationId)
                    .ProjectToEmailNotificationModel().FirstOrDefault();
            }
        }

        public int Insert(EmailNotificationModel model)
        {
            using (var scope = _contextScopeFactory.Create())
            {
                // TODO check devicd group filters

                // update the fields
                var email = new EmailNotification()
                {
                    Disabled = model.Disabled,
                    Email =  model.Email,
                    UserId = model.UserId
                };
                scope.Add(email);
                scope.SaveChanges();
                return email.EmailNotificationId;
            }
        }

        public void Update(EmailNotificationModel model)
        {
            using (var scope = _contextScopeFactory.Create())
            {
                var email = scope
                    .Get<EmailNotification>().FirstOrDefault(d => d.EmailNotificationId == model.EmailNotificationId);
                if (email == null)
                {
                    throw new Exception("Email entry not found: " + model.EmailNotificationId);
                }
                // TODO need to handle filtered device groups

                email.Disabled = model.Disabled;
                email.Email = model.Email;

                scope.SaveChanges();
            }
        }

        public void Delete(int id)
        {
            using (var scope = _contextScopeFactory.Create())
            {
                var emailNotification =
                    scope.Get<EmailNotification>().FirstOrDefault(x => x.EmailNotificationId == id);
                if (emailNotification == null)
                {
                    throw new Exception("Email not found - " + id);
                }
                scope.Delete(emailNotification);
                scope.SaveChanges();
            }
        }

        public List<string> GetEmailsForUser(string email)
        {
            _logger.LogDebug("GetEmailsForUser({0})", email);
            using (var scope = _contextScopeFactory.Create())
            {
                var userId = new UserService(_contextScopeFactory).GetUserIdByEmail(email);
                if (userId == 0)
                {
                    _logger.LogWarning("GetEmailsForUser: no User row found for email '{0}' — user does not exist or email case mismatch in User table", email);
                    return new List<string>();
                }
                _logger.LogDebug("GetEmailsForUser> userId = {0}", userId);
                var results = scope.Get<EmailNotification>().Where(x => x.UserId == userId && x.Disabled != true).Select(x => x.Email).ToList();
                if (results.Count == 0)
                {
                    _logger.LogWarning("GetEmailsForUser: userId {0} ({1}) has no enabled EmailNotification rows — no delivery addresses configured", userId, email);
                }
                foreach (var result in results)
                {
                    _logger.LogDebug("GetEmailsForUser> delivery address = {0}", result);
                }
                return results;
            }
        }
    }
}
