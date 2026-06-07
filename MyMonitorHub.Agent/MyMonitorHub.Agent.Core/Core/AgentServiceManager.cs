using System.Threading;
using Serilog;

namespace MyMonitorHub.Agent.Core
{
    public class AgentServiceManager
    {
        private readonly object _lockObject = new object();
        private bool _stopped = true;

        private Monitor? _monitor;

        private static readonly ILogger Logger = Log.ForContext<AgentServiceManager>();

        public void Start()
        {
            lock (_lockObject)
            {
                if (_stopped)
                {
                    _stopped = false;
                    Logger.Information("Starting the MyMonitorHub Service");

                    _monitor = new Monitor();
                    var thread = new Thread(_monitor.Run);
                    thread.Start();

                    Logger.Information("Successfully started the MyMonitorHub Service");

                }
            } // lock
        }

        public void Stop()
        {
            lock (_lockObject)
            {
                _stopped = true;
                _monitor!.Stop();
            } // lock
            Logger.Information("Successfully stopped the MyMonitorHub Service");
        }

    } // class
}