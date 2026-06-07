using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MyMonitorHub.Agent.Service
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                // Note: the Windows service should run under a dedicated least-privilege
                // gMSA or virtual service account rather than LOCAL SYSTEM. Configure the
                // service account after installation using sc.exe or the Services snap-in.
                Host.CreateDefaultBuilder(args)
                    .UseWindowsService(options => options.ServiceName = "MyMonitorHubAgent")
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        var configDir = Debugger.IsAttached
                            ? Environment.GetEnvironmentVariable("MYMONITORHUB_AGENT_CONFIG_DIR")
                            : null;
                        if (!string.IsNullOrWhiteSpace(configDir))
                        {
                            // Remove sources added by CreateDefaultBuilder so we can
                            // re-add them with the overridden base path.
                            config.Sources.Clear();
                            config.SetBasePath(configDir)
                                  .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                  .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                                  .AddEnvironmentVariables()
                                  .AddCommandLine(args);
                        }
                    })
                    .UseSerilog((context, configuration) =>
                        configuration.ReadFrom.Configuration(context.Configuration))
                    .ConfigureServices(services =>
                    {
                        services.AddHostedService<AgentWorker>();
                    })
                    .Build()
                    .Run();
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
