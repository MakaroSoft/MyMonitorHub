using MyMonitorHub.DataAccess;
using MyMonitorHub.Domain.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmailConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var configDir = Environment.GetEnvironmentVariable("MYMONITORHUB_WEB_CONFIG_DIR")
                ?? throw new InvalidOperationException(
                    "Environment variable 'MYMONITORHUB_WEB_CONFIG_DIR' is not set. " +
                    "Set it to the directory containing the appsettings.json file.");

            var config = new ConfigurationBuilder()
                .SetBasePath(configDir)
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("MonitorContext")
                ?? throw new InvalidOperationException(
                    "Missing 'ConnectionStrings:MonitorContext' in appsettings.json " +
                    $"at '{configDir}'.");

            MonitorDbContextConfig.Configure(connectionString);

            var contextScopeFactory = new DbContextScopeFactory(NullLoggerFactory.Instance);
            var emailer = new Emailer(contextScopeFactory, NullLoggerFactory.Instance);

            var emailTo  = "someone@somewhere.com";
            var subject  = "Email Test2";
            var body     = "This is a test email sent from EmailConsoleTest2.";

            emailer.SendDirect(emailTo, subject, body);

            Console.WriteLine($"Email sent to {emailTo}");
        }
    }
}
