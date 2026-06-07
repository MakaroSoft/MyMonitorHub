using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using MyMonitorHub.Domain.Config;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace MyMonitorHub.Domain.Service
{
    public class Emailer
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        private readonly string _from;
        private readonly string _host;
        private readonly string _password;
        private readonly string _username;

        private readonly IDbContextScopeFactory _dbContextScopeFactory;

        public Emailer(IDbContextScopeFactory dbContextScopeFactory, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<Emailer>();
            _dbContextScopeFactory = dbContextScopeFactory;

            _host     = OutgoingEmailConfig.Host;
            _from     = OutgoingEmailConfig.From;
            _username = OutgoingEmailConfig.Username;
            _password = OutgoingEmailConfig.Password;
        }

        public void SendDirect(string emailTo, string subject, string body)
        {
            var client = new SmtpClient(_host, 587) {Credentials = new NetworkCredential(_username, _password)};
            client.EnableSsl = true;
            var message = new MailMessage(_from, emailTo, subject, body);
            var emailService = new EmailService(_dbContextScopeFactory, _loggerFactory);
            message.Headers.Add("Message-ID", emailService.GetMessageId(_from));
            client.Send(message);
        }

        // used only by password recovery.
        public void SendDirectHTML(string emailTo, string subject, string body)
        {
            var client = new SmtpClient(_host, 587) {Credentials = new NetworkCredential(_username, _password)};
            client.EnableSsl = true;
            var message = new MailMessage(_from, emailTo, subject, body);
            var emailService = new EmailService(_dbContextScopeFactory, _loggerFactory);
            message.Headers.Add("Message-ID", emailService.GetMessageId(_from));
            message.IsBodyHtml = true;
            client.Send(message);
        }
    } // class
} // namespace
