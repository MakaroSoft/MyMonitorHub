using System;
using System.ServiceProcess;
using MyMonitorHub.Common.WebApi;
using MyMonitorHub.Agent.Core;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MyMonitorHub.Agent.Plugins
{
    public class ServiceMonitor : AbstractPlugin
    {
        private static readonly ILogger Logger = Log.ForContext<ServiceMonitor>();

        private string _category;
        private int _count;
        private Service[] _services;

        protected override void Execute()
        {
            if (_services == null)
            {
                _category = Dom.GetAttribute(PluginElement["category"], "name");

                var servicesArray = PluginElement["services"] as JArray;
                if (servicesArray != null)
                {
                    _count = servicesArray.Count;
                    _services = new Service[_count];
                    for (var ind = 0; ind < _count; ind++)
                    {
                        var service = new Service();
                        _services[ind] = service;
                        var item = servicesArray[ind];
                        service.Name = Dom.GetAttribute(item, "name");
                        service.SubCategory = Dom.GetAttribute(item, "sub-category");
                        Logger.Information("    " + Title + " - Registering service name: " + service.Name);
                    }
                }
            }

            for (var index = 0; index < _count; index++)
            {
                if (_services == null) break; // should never happen

                var service = _services[index];

                EventType theStatus;
                string desc;
                try
                {
                    theStatus = EventType.Ok;
                    desc = $"{service.Name} is running";

                    var controller = new ServiceController { MachineName = ".", ServiceName = service.Name };
                    if (controller.Status == ServiceControllerStatus.Running)
                    {
                        service.Count = 0;
                    }
                    else
                    {
                        service.Count++;
                        if (service.Count > 1)
                        {
                            // this makes it so that if we detect a fail, wait another two minutes and try again
                            //     thus eliminating occurrences of quick stop/starts which I don't care about.
                            //     The downfall is it takes 4 minutes to detect a downed service as apposed to 2 minutes before.
                            theStatus = EventType.Fail;
                            desc = $"{service.Name} is in {controller.Status} state";
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    theStatus = EventType.Fail;
                    desc = $"{service.Name} - Invalid Operation";
                }

                // remember we poll every 2 minutes but don't always send back a response. only if first time, 
                // state has changed(ok/fail), or statistics collection(every 10 minutes)
                if (SayStatus(_category, service.SubCategory, service.Name, desc, theStatus))
                {
                }
            }
        }




        public class Service
        {
            public string Name { get; set; }
            public string SubCategory { get; set; }
            public int Count { get; set; }
        }

    }
}