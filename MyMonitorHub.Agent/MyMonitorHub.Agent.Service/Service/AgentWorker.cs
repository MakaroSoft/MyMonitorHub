using System.Threading;
using System.Threading.Tasks;
using MyMonitorHub.Agent.Core;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MyMonitorHub.Agent.Service
{
    public class AgentWorker : BackgroundService
    {
        private static readonly ILogger Logger = Log.ForContext<AgentWorker>();
        private readonly AgentServiceManager _agentServiceManager = new AgentServiceManager();

        public AgentWorker()
        {
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.Information("AgentWorker starting");

            _agentServiceManager.Start();

            stoppingToken.Register(() =>
            {
                Logger.Information("AgentWorker stopping");
                _agentServiceManager.Stop();
            });

            return Task.CompletedTask;
        }
    }
}
