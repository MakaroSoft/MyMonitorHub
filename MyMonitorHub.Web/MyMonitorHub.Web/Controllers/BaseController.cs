using System.IO;
using System.Text;
using System.Threading.Tasks;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Service;
using MyMonitorHub.Domain.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace MyMonitorHub.Web.Controllers
{
    public class BaseController : Controller
    {
        private readonly IDbContextScopeFactory? _dbContextScopeFactory;
        private readonly ICompositeViewEngine? _viewEngine;
        private readonly ITempDataProvider? _tempDataProvider;

        protected BaseController() { }

        protected BaseController(IDbContextScopeFactory dbContextScopeFactory)
        {
            _dbContextScopeFactory = dbContextScopeFactory;
        }

        /// <summary>Constructor used when view-rendering helpers are also needed.</summary>
        protected BaseController(
            IDbContextScopeFactory dbContextScopeFactory,
            ICompositeViewEngine viewEngine,
            ITempDataProvider tempDataProvider)
        {
            _dbContextScopeFactory = dbContextScopeFactory;
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
        }

        internal JsonResult JsonWarning(string message)
            => Json(new { JsonWarning = message });

        /// <summary>Renders a Razor view to a string (used for email body generation).</summary>
        public async Task<string> RenderRazorViewToString(string viewName, object? model)
        {
            if (_viewEngine == null)
                throw new InvalidOperationException(
                    "ICompositeViewEngine must be injected to use RenderRazorViewToString.");

            ViewData.Model = model;
            using var sw = new StringWriter();

            // Use GetView for application-relative paths (~/...) and FindView for named views.
            ViewEngineResult viewResult = (viewName.StartsWith("~/") || viewName.StartsWith("/"))
                ? _viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: false)
                : _viewEngine.FindView(ControllerContext, viewName, isMainPage: false);

            if (viewResult.View == null)
                throw new FileNotFoundException($"View '{viewName}' not found.");

            var viewContext = new ViewContext(
                ControllerContext,
                viewResult.View,
                ViewData,
                TempData,
                sw,
                new HtmlHelperOptions());

            await viewResult.View.RenderAsync(viewContext);
            return sw.ToString();
        }

        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            if (User.Identity?.IsAuthenticated != true) return;
            if (_dbContextScopeFactory == null) return;

            // The auth cookie can outlive the server-side session (e.g. after an app restart
            // or session expiry). When that happens, User.Identity.IsAuthenticated is still
            // true but the session has lost UserId/AccountId/RoleDocument/etc., which causes
            // an empty Devices menu and disabled Administration items in the layout.
            // Rehydrate the session from the authenticated email before any action runs.
            if (Helper.UserId <= 0 && !string.IsNullOrEmpty(User.Identity.Name))
            {
                try
                {
                    Helper.Setup(_dbContextScopeFactory, User.Identity.Name);
                }
                catch
                {
                    // If the user can't be rehydrated (e.g. account removed), fall through
                    // and let the request continue with an empty session.
                }
                return;
            }

            if (Helper.CurrentOrganizationInfo == null)
            {
                var orgInfo = new OrganizationService(_dbContextScopeFactory)
                    .GetDefaultOrganization(Helper.UserId);
                if (orgInfo != null)
                {
                    Helper.CurrentOrganizationInfo = orgInfo;
                    Helper.SetupMember(_dbContextScopeFactory);
                }
            }
        }
    }
}
