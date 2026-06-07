using MyMonitorHub.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace MyMonitorHub.Web.Controllers
{
    public class WelcomeController : BaseController
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;

        public WelcomeController(IDbContextScopeFactory contextScopeFactory)
            : base(contextScopeFactory)
        {
            _contextScopeFactory = contextScopeFactory;
        }

        public IActionResult Index() => View();
    }
}
