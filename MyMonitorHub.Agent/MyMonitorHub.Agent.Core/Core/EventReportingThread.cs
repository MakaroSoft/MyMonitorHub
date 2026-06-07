using System;
using System.Diagnostics;
using System.Threading;
using System.Xml;
using MyMonitorHub.Common.WebApi;
using MyMonitorHub.Agent.Common;
using Serilog;

namespace MyMonitorHub.Agent.Core
{
    /// <summary>
    ///     Checks on the queue of events every minute and sends what it finds out to the monitoring
    ///     server.
    /// </summary>
    internal sealed class EventReportingThread : IThreadShutdown
    {
        private static readonly ILogger Logger = Log.ForContext<EventReportingThread>();


        private volatile bool _stopped = true;
        private readonly object _padlock = new object();

        private DateTime _lastSuccess;


        private void Run()
        {
            _lastSuccess = DateTime.Now;
            Logger.Information("Start of Event reporting thread");

            _stopped = false;
            while (!_stopped)
            {
                // wait 60 seconds before starting
                try
                {
                    lock (_padlock)
                    {
                        System.Threading.Monitor.Wait(_padlock, 60000);
                    }
                    if (_stopped)
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    _stopped = true;
                    break;
                }

                var start = DateTime.Now;

                var eventList = Monitor.EventList; // synced and creates a new empy list
                if (eventList.Count != 0)
                {
                    var packet = new EventPacket
                        {
                            Events = eventList.ToArray()
                        };

                    try
                    {
                        var service = new WebApiCall(MonitorProfile.Current.BaseAddress, MonitorProfile.TokenManager);
                        service.Send<bool>(MonitorProfile.Current.AccountId,
                            MonitorProfile.Current.DeviceId,
                            packet);

                        _lastSuccess = DateTime.Now;
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Choked on sending the event: {Message}", e.Message);
                        if (e.InnerException != null)
                        {
                            Logger.Error("Inner Exception: {Message}", e.InnerException.Message);
                        }

                        DisplayTimeToExecuteRequest(start, true);
                        var reconnectAttemptMinutes = MonitorProfile.Current.ReConnectAttemptMinutes;
                        if (reconnectAttemptMinutes > 0)
                        {
                            var diff = DateTime.Now - _lastSuccess;
                            if (diff.TotalMinutes >= reconnectAttemptMinutes)
                            {
                                Logger.Error("*** Messages are not getting out! Tring to restarting the service!");
                                Thread.Sleep(2000); // if logger uses a differnt thread(???) then give it time to finish
                                lock (Monitor.FileWriteLock)
                                {
                                    // i know monitor.json is not being written out from any threads in this program
                                    Process.GetCurrentProcess().Kill();
                                }
                            }
                        }
                    }
                }

            } // while
            Logger.Information("Event reporting thread has come to an end");
        }

        private void DisplayTimeToExecuteRequest(DateTime start, bool overrid)
        {
            var end = DateTime.Now;
            var duration = (end - start).TotalMilliseconds;
            duration = duration/1000; // convert to seconds

            // if the duration took longer than 30 seconds then I want to know about it.
            if (duration > 30 || overrid)
            {
                Logger.Information("time in seconds to execute the request: " + duration);
            }
        }

        private Thread? _thread;
        internal void Start()
        {
            _thread = new Thread(Run);
            _thread.Start();
        }
        public void Stop()
        {
            _stopped = true;
            lock (_padlock)
            {
                System.Threading.Monitor.Pulse(_padlock);
            }
            _thread?.Join(10000); // wait a max of 10 seconds to shut this down
        }
    }
}