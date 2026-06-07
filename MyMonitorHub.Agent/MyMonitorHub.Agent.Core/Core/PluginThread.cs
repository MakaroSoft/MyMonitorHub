using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MyMonitorHub.Agent.Core
{
    public class PluginThread : IThreadShutdown
    {
        private static readonly ILogger Logger = Log.ForContext<PluginThread>();

        private const int MinimumSleep = 30000;
        private readonly bool _multi;
        private string? _assemblyName;
        private string? _className;
        private int _hibernateSeconds;

        private DateTime _lastRun;
        internal AbstractPlugin? Plugin;
        private string? _title;

        private volatile bool _stopped = true;
        private readonly object _padlock = new object();

        public PluginThread(int index, bool multi)
        {
            // this constructor is only used from GroupPluginThread
            _multi = multi;

            var config = Monitor.ReadConfig();

            if (multi)
            {
                var groupPluginArray = config["group-plugins"] as JArray;
                if (groupPluginArray != null && index < groupPluginArray.Count)
                {
                    Init(groupPluginArray[index]);
                }
            }
            else
            {
                var pluginArray = config["plugins"] as JArray;
                if (pluginArray != null && index < pluginArray.Count)
                {
                    Init(pluginArray[index]);
                }
            }
        }

        private int SleepTime
        {
            get
            {
                var hibernateMillis = Plugin!.HibernateSeconds*(long) 1000;

                var now = DateTime.Now;
                var usedMillis = (now - _lastRun).TotalMilliseconds;

                var sleepTime = hibernateMillis - usedMillis;
                if (sleepTime < MinimumSleep)
                {
                    sleepTime = MinimumSleep;
                }

                return (int)sleepTime;
            }
        }

        private void Init(JToken element)
        {
            _title = element["title"]?.ToString();
            _className = element["class-name"]?.ToString();
            _assemblyName = element["assembly"]?.ToString();

            if (_assemblyName != null)
            {
                Logger.Information("assemblyName is: {0}", _assemblyName);
            }

            object? o = null;
            try
            {
                Assembly assembly;
                if (_assemblyName == null)
                {
                    assembly = GetType().Assembly;
                }
                else
                {
                    var baseDir = Path.GetFullPath(AppContext.BaseDirectory);
                    var assemblyPath = Path.GetFullPath(
                        Path.IsPathRooted(_assemblyName)
                            ? _assemblyName
                            : Path.Combine(baseDir, _assemblyName));

                    if (!assemblyPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            $"Plugin path '{assemblyPath}' is outside the install directory and cannot be loaded.");

                    assembly = Assembly.LoadFrom(assemblyPath);
                }
                // create an instance of the class from the assembly
                o = assembly.CreateInstance(_className!);
            }
            catch (Exception e)
            {
                Logger.Error("Class name is: {0}", _className);
                Logger.Error(e.Message + "\r\n" + e.StackTrace);
                if (e.InnerException != null)
                {
                    Logger.Error(e.InnerException.Message + "\r\n" + e.InnerException.StackTrace);
                    if (e.InnerException.InnerException != null)
                    {
                        Logger.Error(e.InnerException.InnerException.Message + "\r\n" + e.InnerException.InnerException.StackTrace);
                    }
                }
            }

            if (!(o is AbstractPlugin))
            {
                throw new Exception($"class ({_className}) must implement AbstractPlugin");
            }
            Plugin = (AbstractPlugin)o;
            Plugin.Initialize(element, _title ?? string.Empty);
        }

        public void Run()
        {
            _hibernateSeconds = Plugin!.HibernateSeconds;
            Logger.Information("starting " + _title + " thread. Hibernation time = " + _hibernateSeconds + " seconds.");
            if (_multi)
            {
                var groupPlugin = (GroupPlugin) Plugin;
                groupPlugin.DisplayPlugins();
            }
            _stopped = false;
            while (!_stopped)
            {
                _lastRun = DateTime.Now;
                try
                {
                    Plugin.Start();
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Plugin execution error");
                }
                try
                {
                    var sleepTime = SleepTime;
                    Logger.Debug("going to sleep for " + sleepTime/1000 + " seconds");
                    lock (_padlock)
                    {
                        System.Threading.Monitor.Wait(_padlock, sleepTime);
                    }
                }
                catch (Exception e)
                {
                    _stopped = true;
                    Logger.Error(e, "Plugin thread wait interrupted");
                }
            } // while
            if (_multi)
            {
                Logger.Information("MultiPluginThread: " + _title + " - has come to an end");
            }
            else
            {
                Logger.Information("PluginThread: " + _title + " - has come to an end");
            }
        }

        private Thread? _thread;
        public void Start()
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

    // class
}