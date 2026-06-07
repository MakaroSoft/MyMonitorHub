using MyMonitorHub.Domain.Interface;
using Microsoft.AspNetCore.Mvc;

namespace MyMonitorHub.Web.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(IDbContextScopeFactory dbContextScopeFactory)
            : base(dbContextScopeFactory)
        {
        }

        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Summary");

            return RedirectToAction("LogOn", "Account");
        }

        [HttpPost]
        public JsonResult KeepSessionAlive() => Json("Success");
    }
}
