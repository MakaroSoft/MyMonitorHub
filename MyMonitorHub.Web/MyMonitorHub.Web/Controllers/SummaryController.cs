using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

namespace MyMonitorHub.Web.Controllers
{
        [Authorize]
    public class SummaryController : BaseController
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;
        private readonly ILoggerFactory _loggerFactory;

        public SummaryController(IDbContextScopeFactory contextScopeFactory, ILoggerFactory loggerFactory)
            : base(contextScopeFactory)
        {
            _loggerFactory = loggerFactory;
            _contextScopeFactory = contextScopeFactory;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var data = new ServiceRequestService(_contextScopeFactory, _loggerFactory).GetOutstanding();
            return View(data);
        }
    }
}
