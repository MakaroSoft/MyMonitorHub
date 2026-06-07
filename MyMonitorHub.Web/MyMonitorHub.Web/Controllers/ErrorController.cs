using Microsoft.AspNetCore.Mvc;

namespace MyMonitorHub.Web.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult Custom() => View();
        public IActionResult OrganizationError() => View();
    }
}
