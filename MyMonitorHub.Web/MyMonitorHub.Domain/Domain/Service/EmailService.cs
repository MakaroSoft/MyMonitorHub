using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using MyMonitorHub.Domain.BO.Transfer;
using MyMonitorHub.Domain.Config;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Util;
using Microsoft.Extensions.Logging;
using NReco.PdfGenerator;

namespace MyMonitorHub.Domain.Service
{
    public class EmailService
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDbContextScopeFactory _contextScopeFactory;
        public EmailService(IDbContextScopeFactory contextScopeFactory, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<EmailService>();
            _contextScopeFactory = contextScopeFactory;
        }

        /// <summary>
        /// Sends a "service request closed" notification email using pre-rendered HTML strings.
        /// Both <paramref name="emailBodyHtml"/> and <paramref name="pdfHtml"/> are rendered
        /// in the web layer (ServiceRequestController) so that no unauthenticated HTTP
        /// self-fetches are required.
        /// </summary>
        public void AlertCustomerRequestIsClosed(string emailList, int serviceRequestId, int accountId, string emailBodyHtml, string pdfHtml, string pathBase = "")
        {
            string path;
            try
            {
                path = GenerateSrClosedPdf(serviceRequestId, accountId, pdfHtml, pathBase);
            }
            catch (Exception e)
            {
                var msg = "Error trying to generate PDF for sr" + serviceRequestId + "\r\n" +
                             e.Message + "\r\n" +
                             e.StackTrace;
                _logger.LogError(msg);
                return;
            }

            try
            {
                using (var scope = _contextScopeFactory.Create())
                {
                    var client = new SmtpClient(OutgoingEmailConfig.Host, 587)
                    {
                        EnableSsl = true,
                        Credentials = new NetworkCredential(OutgoingEmailConfig.Username, OutgoingEmailConfig.Password)
                    };

                    var message = new MailMessage {From = new MailAddress(OutgoingEmailConfig.From)};
                    message.Headers.Add("Message-ID", GetMessageId(OutgoingEmailConfig.From));

                    var append = "sr" + serviceRequestId;

                    try
                    {
                        var groupDesc =
                            scope.Get<ServiceRequest>().FirstOrDefault(x => x.ServiceRequestId == serviceRequestId)
                                ?.Device.DeviceGroup.Description;
                        append = groupDesc;
// ReSharper disable once EmptyGeneralCatchClause
                    }
                    catch
                    {
                    }

                    message.Subject = "A monitoring alert has been closed for " + append;
                    message.Body = emailBodyHtml;
                    message.IsBodyHtml = true;

                    if (string.IsNullOrEmpty(emailList))
                    {
                        emailList = OutgoingEmailConfig.From;
                        message.Subject = "(NSTC)" + message.Subject;
                    }
                    else
                    {
                        message.Bcc.Add(new MailAddress(OutgoingEmailConfig.From));
                    }

                    var emails = emailList.Split(new[] {';'});
                    foreach (var email in emails)
                    {
                        if (email.Trim() != "")
                        {
                            message.To.Add(new MailAddress(email.Trim()));
                        }
                    } // foreach

                    var attachment = new Attachment(path);
                    message.Attachments.Add(attachment);

                    client.Send(message);
                } // using
            }
            catch (Exception e)
            {
                var msg = "Error trying to send email for sr" + serviceRequestId + " to email address: (" + emailList +
                             ")\r\n" +
                             e.Message + "\r\n" +
                             e.StackTrace;
                _logger.LogError(msg);
            } // try
        }

        /// <summary>
        /// Prepares HTML for in-process PDF rendering by wkhtmltopdf (NReco).
        /// Rewrites root-relative URLs to relative form and injects a file:// base href
        /// so wkhtmltopdf loads CSS/images directly from disk.
        /// When the app is hosted as an IIS sub-application, ASP.NET Core tag helpers prepend
        /// the PathBase (e.g. /mymonitorhub) to every ~/... URL. This method strips that prefix
        /// before stripping the remaining leading slash so wkhtmltopdf resolves resources
        /// relative to wwwroot correctly.
        /// </summary>
        private static string PrepareHtmlForPdf(string html, string rootPath, string pathBase = "")
        {
            if (string.IsNullOrEmpty(html) || string.IsNullOrEmpty(rootPath)) return html;

            var normalizedBase = pathBase?.Trim('/') ?? string.Empty;
            if (!string.IsNullOrEmpty(normalizedBase))
            {
                html = System.Text.RegularExpressions.Regex.Replace(
                    html,
                    $"(?<attr>href|src)=\"/{System.Text.RegularExpressions.Regex.Escape(normalizedBase)}/",
                    "${attr}=\"");
            }

            html = System.Text.RegularExpressions.Regex.Replace(
                html,
                "(?<attr>href|src)=\"/(?!/)",
                "${attr}=\"");

            var idx = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return html;
            var fileBase = "file:///" + rootPath.Replace('\\', '/').TrimEnd('/') + "/";
            return html.Insert(idx + "<head>".Length, $"<base href=\"{fileBase}\" />");
        }

        private string GenerateSrClosedPdf(int serviceRequestId, int accountId, string html, string pathBase = "")
        {
            try
            {
                var deviceGroupId = 0;
                using (var scope = _contextScopeFactory.Create())
                {
                    var nullableDeviceGroupId = scope.Get<ServiceRequest>()
                        .FirstOrDefault(x => x.AccountId == accountId && x.ServiceRequestId == serviceRequestId)
                        ?.Device
                        .DeviceGroup.DeviceGroupId;

                    if (nullableDeviceGroupId != null)
                        deviceGroupId = nullableDeviceGroupId.Value;
                }

                var path = Path.Combine(ThreadStaticHelper.RootPath,
                    "Repository" + Path.DirectorySeparatorChar + deviceGroupId + Path.DirectorySeparatorChar +
                    "closedSRs");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                var physicalPath = Path.Combine(ThreadStaticHelper.RootPath,
                    "repository" + Path.DirectorySeparatorChar + deviceGroupId + Path.DirectorySeparatorChar +
                    "closedSRs" + Path.DirectorySeparatorChar + "sr" + serviceRequestId + ".pdf");

                var preparedHtml = PrepareHtmlForPdf(html, ThreadStaticHelper.RootPath, pathBase);
                var htmlToPdf = new HtmlToPdfConverter
                {
                    CustomWkHtmlArgs = "--enable-local-file-access"
                };
                var pdfBytes = htmlToPdf.GeneratePdf(preparedHtml);
                File.WriteAllBytes(physicalPath, pdfBytes);
                return physicalPath;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occurred");
                throw;
            }
        }









        public string GetMessageId(string from)
        {
            var fromPart = from.Substring(@from.IndexOf("@", StringComparison.Ordinal));
            return "<" + DateTime.Now.ToString("yyyyMMdd-HHmmssfff") + fromPart + ">";
        }

        public void RequestUpdatedInformation(int deviceGroupId)
        {
            var deviceData = new DeviceGroupService(_contextScopeFactory).GetData(deviceGroupId);
            var page = ThreadStaticHelper.RootUrl + "email/InfoRequest/" + deviceGroupId;

            _logger.LogDebug("Trying to read page: " + page);

            var wr = (HttpWebRequest)WebRequest.Create(page);
            var htmlBody = (new
                // ReSharper disable once AssignNullToNotNullAttribute
                StreamReader(wr.GetResponse().GetResponseStream())).ReadToEnd();

            var emails = string.Empty;
            var accountId = deviceData.AccountId;
            SendAway(emails, "Please provide updated information for ADS monitoring support", null, htmlBody);
        }

        public void SendMonthlyReport(int deviceGroupId,int year, int month, string attachmentFileName)
        {

            var deviceData = new DeviceGroupService(_contextScopeFactory).GetData(deviceGroupId);

            var page = ThreadStaticHelper.RootUrl + "email/MonthlyReport/" + deviceData.DeviceGroupId;

            _logger.LogDebug("Trying to read page: "+page);
            _logger.LogDebug("attachment file name is: " + attachmentFileName);

            var wr = (HttpWebRequest)WebRequest.Create(page);
            var htmlBody = (new
                // ReSharper disable once AssignNullToNotNullAttribute
                StreamReader(wr.GetResponse().GetResponseStream())).ReadToEnd();

            var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
            var subject = monthName + "-" + year + " ADS Monitoring report for " + deviceData.Description;

            var emails = string.Empty;
            var accountId = deviceData.AccountId;
            SendAway(emails, subject, attachmentFileName, htmlBody);

            var exists = new MonthlyReportService(_contextScopeFactory, _loggerFactory).Exists(deviceGroupId, year, month);
            if (!exists)
            {
                var r = new MonthlyReport
                {
                    AccountId = deviceData.AccountId,
                    DeviceGroupId = deviceData.DeviceGroupId,
                    Month = month,
                    Year = year
                };
                new MonthlyReportService(_contextScopeFactory, _loggerFactory).Insert(r);
            }

        }

        public void SendAway(string emailList, string subject, string attachmentFileName, string htmlBody)
        {
            try
            {
                var from = OutgoingEmailConfig.From;

                var client = new SmtpClient(OutgoingEmailConfig.Host, 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(OutgoingEmailConfig.Username, OutgoingEmailConfig.Password)
                };

                // create the email message
                var message = new MailMessage { From = new MailAddress(from) };
                message.Headers.Add("Message-ID", GetMessageId(from));

                message.Subject = subject;
                message.Body = htmlBody;
                message.IsBodyHtml = true;

                if (string.IsNullOrEmpty(emailList))
                {
                    emailList = from;
                    message.Subject = "(NSTC)" + message.Subject;
                }
                else
                {
                    // for now blind carbon copy me
                    message.Bcc.Add(new MailAddress(from));
                }

                // add all the email recipients
                var emails = emailList.Split(';');
                foreach (var email in emails)
                {
                    if (email.Trim() != "")
                    {
                        message.To.Add(new MailAddress(email.Trim()));
                    }
                } // foreach

                if (attachmentFileName != null)
                {
                    var attachment = new Attachment(attachmentFileName);
                    message.Attachments.Add(attachment);
                }

                // now send the email
                client.Send(message);
            }
            catch (Exception e)
            {
                var msg = "Error trying to send to email address: (" + emailList + ")\r\n" +
                          e.Message + "\r\n" +
                          e.StackTrace;
                _logger.LogError(msg);
                throw;
            } // try
        }

    } // class
} // namespace