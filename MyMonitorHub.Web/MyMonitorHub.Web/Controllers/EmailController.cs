using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

namespace MyMonitorHub.Web.Controllers
{
    [Authorize]
        public class EmailController : BaseController
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;
        private readonly ILoggerFactory _loggerFactory;

        public EmailController(IDbContextScopeFactory contextScopeFactory, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _contextScopeFactory = contextScopeFactory;
        }

        [HttpGet]
        public IActionResult SrClosed(int id)
        {
            var data = new ServiceRequestService(_contextScopeFactory, _loggerFactory).SrClosed(id);
            return View(data);
        }

        [HttpGet]
        public IActionResult MonthlyReport(int id)
        {
            ViewBag.ImageUrl = $"{Request.Scheme}://{Request.Host}/Content/ms/images/Logo.png";
            return View();
        }

        public IActionResult InfoRequest(int id)
        {
            ViewBag.ImageUrl = $"{Request.Scheme}://{Request.Host}/Content/ms/images/Logo.png";

            var contacts = new ContactService(_contextScopeFactory, _loggerFactory).GetContacts(id);
            foreach (var contact in contacts)
            {
                if (string.IsNullOrEmpty(contact.Notes)) contact.Notes = "__________";
            }
            ViewBag.Contacts = contacts;

            var data = new DeviceGroupService(_contextScopeFactory).GetData(id);
            if (string.IsNullOrEmpty(data.emailsForClosedSRs)) data.emailsForClosedSRs = "__________";
            ViewBag.Emails = data;
            return View();
        }
    }
}
