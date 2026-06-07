using MyMonitorHub.Domain.Interface;

namespace MyMonitorHub.Web.Controllers
{
    public class LogoService : ILogoService
    {
        public string GetLogo()
        {
            return "~/Content/Organization/MyMonitorHub.png";
        }
    }
}