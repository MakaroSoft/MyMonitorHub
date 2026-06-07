using MyMonitorHub.DataAccess;
using MyMonitorHub.Domain.BO.Pdf;
using MyMonitorHub.Domain.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace PdfConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("MonitorDb")
                ?? throw new InvalidOperationException(
                    "Missing 'ConnectionStrings:MonitorDb'. " +
                    "Run: dotnet user-secrets set \"ConnectionStrings:MonitorDb\" \"<your connection string>\"");

            // Allow an explicit override, but default to the web project's wwwroot
            // by walking up from the binary until we find the solution root (.sln file).
            var wwwrootPath = config["WwwrootPath"];
            if (string.IsNullOrEmpty(wwwrootPath))
            {
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                while (dir != null && !dir.GetFiles("*.sln").Any())
                    dir = dir.Parent;

                if (dir == null)
                    throw new InvalidOperationException(
                        "Could not locate the solution root. Set 'WwwrootPath' in user secrets: " +
                        "dotnet user-secrets set \"WwwrootPath\" \"<path to wwwroot>\"");

                wwwrootPath = Path.Combine(dir.FullName, "MyMonitorHub.Web", "wwwroot");
            }

            var accountId       = int.Parse(config["AccountId"]       ?? "1001");
            var serviceRequestId = int.Parse(config["ServiceRequestId"] ?? "50156");

            MonitorDbContextConfig.Configure(connectionString);
            ThreadStaticHelper.RootPath = wwwrootPath;
            Haley.RotateText.ImageGenerator.WebRootPath = wwwrootPath;

            var contextScopeFactory = new DbContextScopeFactory(NullLoggerFactory.Instance);

            Console.WriteLine($"Generating PDF for AccountId={accountId}, ServiceRequestId={serviceRequestId}...");

            try
            {
                var report = new Report(contextScopeFactory);
                report.SRClosed(accountId, serviceRequestId);
                Console.WriteLine($"PDF generated successfully: {report.PDFPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
