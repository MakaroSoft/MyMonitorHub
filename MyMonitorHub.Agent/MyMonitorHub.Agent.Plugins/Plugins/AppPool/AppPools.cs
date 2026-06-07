using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Web.Administration;

namespace MyMonitorHub.Agent.Plugins.AppPool
{
    public class AppPools
    {
        private readonly List<AppPool> _appPools;

        public AppPools()
        {
            var server = new ServerManager();
            var applicationPools = server.ApplicationPools;

            _appPools = new List<AppPool>();

            foreach (var pool in applicationPools)
            {
                _appPools.Add(new AppPool
                {
                    Name = pool.Name,
                    State = pool.State
                });
            }
        }

        private class AppPool
        {
            public string Name { get; set; }
            public ObjectState State { get; set; }

            public override string ToString()
            {
                return $"Name = {Name}, State = {State}";
            }
        }

        public bool IsRunning(string appName)
        {
            var appPool = _appPools.FirstOrDefault(x => x.Name == appName);
            if (appPool == null) return false;
            return appPool.State == ObjectState.Started;
        }

        public ObjectState GetState(string appName)
        {
            var appPool = _appPools.FirstOrDefault(x => x.Name == appName);
            if (appPool == null) return ObjectState.Unknown;
            return appPool.State;

        }

    }
}
