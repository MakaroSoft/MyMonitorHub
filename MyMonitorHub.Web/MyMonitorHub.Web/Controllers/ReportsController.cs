using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Service;
using MyMonitorHub.Domain.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyMonitorHub.Web.Controllers
{
    [Authorize]
    public class ReportsController : BaseController
    {
        private readonly IDbContextScopeFactory _dbContextScopeFactory;

        public ReportsController(IDbContextScopeFactory dbContextScopeFactory)
            : base(dbContextScopeFactory)
        {
            _dbContextScopeFactory = dbContextScopeFactory;
        }

        public IActionResult HtmlFormatOfSrReport(int id)
        {
            ILogoService logoService = new LogoService();
            var srReportService = new SrReportService(_dbContextScopeFactory, logoService);
            var srReportModel = srReportService.GetSrReportModel(id, Helper.AccountId);
            ViewBag.ItemLayoutView = new ItemService(_dbContextScopeFactory).GetItemLayout(srReportModel.PageId, -1, srReportModel.DeviceId, false, Request.PathBase.ToString());
            return View("~/Views/Reports/ServiceRequest/HtmlFormatOfSrReport.cshtml", srReportModel);
        }

        public IActionResult DiskGraph(int id, string report, string date)
        {
            var service = new DiskGraphService(_dbContextScopeFactory);
            var stream = service.RenderGraph(id, report, date);
            return new FileStreamResult(stream, "image/png");
        }

        public IActionResult ResponseGraph(int id, string report, string date)
        {
            var service = new ResponseGraphService(_dbContextScopeFactory);
            var stream = service.RenderGraph(id, report, date);
            return new FileStreamResult(stream, "image/png");
        }
    }
}
