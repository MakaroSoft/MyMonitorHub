using System;
using MyMonitorHub.Common.WebApi;
using MyMonitorHub.Agent.Core;
using MyMonitorHub.Agent.Plugins.AppPool;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MyMonitorHub.Agent.Plugins
{
    public class AppPoolMonitor : AbstractPlugin
    {
        private static readonly ILogger Logger = Log.ForContext<AppPoolMonitor>();

        private string _category;
        private int _count;
        private AppPool[] _appPools;

        protected override void Execute()
        {
            if (_appPools == null)
            {
                _category = Dom.GetAttribute(PluginElement["category"], "name");

                var appPoolsArray = PluginElement["app-pools"] as JArray;
                if (appPoolsArray != null)
                {
                    _count = appPoolsArray.Count;
                    _appPools = new AppPool[_count];
                    for (var ind = 0; ind < _count; ind++)
                    {
                        var appPool = new AppPool();
                        _appPools[ind] = appPool;
                        var item = appPoolsArray[ind];
                        appPool.Name = Dom.GetAttribute(item, "name");
                        appPool.SubCategory = Dom.GetAttribute(item, "sub-category");
                        Logger.Information("    " + Title + " - Registering app pool name: " + appPool.Name);
                    }
                }
            }

            AppPools appPools;
            try
            {
                appPools = new AppPools();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            for (var index = 0; index < _count; index++)
            {
                if (_appPools == null) break; // should never happen

                var appPool = _appPools[index];

                EventType theStatus;
                string desc;

                if (appPools.IsRunning(appPool.Name))
                {
                    theStatus = EventType.Ok;
                    desc = $"{appPool.Name} is running";
                }
                else
                {
                    theStatus = EventType.Fail;
                    desc = $"{appPool.Name} is in {appPools.GetState(appPool.Name)} state";
                }

                // remember we poll every 2 minutes but don't always send back a response. only if first time, 
                // state has changed(ok/fail), or statistics collection(every 10 minutes)
                if (SayStatus(_category, appPool.SubCategory, appPool.Name, desc, theStatus))
                {
                }
            }
        }




        public class AppPool
        {
            public string Name { get; set; }
            public string SubCategory { get; set; }
        }

    }
}