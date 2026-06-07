using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Exceptions;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Util;

namespace MyMonitorHub.Domain.Service
{
    public class CpuService
    {
        private readonly IDbContextScopeFactory _dbContextScopeFactory;

        public CpuService(IDbContextScopeFactory dbContextScopeFactory)
        {
            _dbContextScopeFactory = dbContextScopeFactory;
        }

        public CpuDataModel GetDay(int itemId, string date)
        {
            if (!Authorizer.IsAdministrator)
            {
                var securityInfo = new ItemService(_dbContextScopeFactory).GetSecurityInfo(itemId);

                if (securityInfo == null)
                {
                    throw new Exception("Invalid data.");
                }


                if (securityInfo.accountId != Helper.AccountId)
                {
                    throw new SecurityException("You only have permission to access your own account pages.");
                }
                // filter based on permissions
                if (securityInfo.deviceGroupId == 0)
                {
                    throw new SecurityException("Access denied.");
                }

            }

            return DisplayDay(itemId, date);
        }
        public CpuDataModel GetHour(int itemId, string date, int hour)
        {
            if (!Authorizer.IsAdministrator)
            {
                var securityInfo = new ItemService(_dbContextScopeFactory).GetSecurityInfo(itemId);

                if (securityInfo == null)
                {
                    throw new Exception("Invalid data.");
                }


                if (securityInfo.accountId != Helper.AccountId)
                {
                    throw new SecurityException("You only have permission to access your own account pages.");
                }
                // filter based on permissions
                if (securityInfo.deviceGroupId == 0)
                {
                    throw new SecurityException("Access denied.");
                }

            }

            return DisplayHour(itemId, date, hour);
        }

        private CpuDataModel DisplayDay(int itemId, string date)
        {
            var startDate = DateTime.Now.Date;
            if (!string.IsNullOrEmpty(date))
            {
                startDate = DateTime.Parse(date);
            }

            // collect the json data from the database
            var detail = GetDataForDay(itemId, startDate);

            // convert to TopCPU objects
            var topCpus = detail.Select(entry => new {
                Timestamp = entry.Timestamp,
                TopCpu = JsonSerializer.Deserialize<TopCpu>(entry.JsonData)
                })
                .ToList();

            // count the number of cpus
            var maxCpus = topCpus.Max(x => x.TopCpu.percentages.Length);
            var maxTicks = 17280; // ticks per day. 1 tick = 5 seconds    //topCpus.Max(x => x.percentages.Max(y => y.cpuPercentages.Length));

            var cpuObjects = new CpuDataModel
            {
                cpus = new Cpu[maxCpus]
            };
            // initialize the cpu collectors
            for (var index = 0; index < maxCpus; index++)
            {
                cpuObjects.cpus[index] = new Cpu {Ticks = new int[maxTicks]};
            }

            foreach (var topCpu in topCpus)
            {
                for (var index = 0; index < topCpu.TopCpu.percentages.Length; index++)
                {
                    var secondsAfterMidnight = (int)topCpu.Timestamp.TimeOfDay.TotalSeconds;
                    var startTick = secondsAfterMidnight/5;
                    var percentages = topCpu.TopCpu.percentages[index];
                    var t = 0;
                    foreach (var tick in percentages.cpuPercentages)
                    {
                        if (startTick + t >= 0 && startTick + t < maxTicks)
                        {
                            cpuObjects.cpus[index].Ticks[startTick + t] = tick;
                        }
                        t++;
                    }
                }
            }
            return cpuObjects;
        }

        private CpuDataModel DisplayHour(int itemId, string date, int hour)
        {
            var startDate = DateTime.Now.Date;
            if (!string.IsNullOrEmpty(date))
            {
                startDate = DateTime.Parse(date);
            }

            // collect the json data from the database
            var detail = GetDataForHour(itemId, startDate, hour);

            // convert to TopCPU objects
            var topCpus = detail.Select(entry => new {
                Timestamp = entry.Timestamp,
                TopCpu = JsonSerializer.Deserialize<TopCpu>(entry.JsonData)
            })
                .ToList();

            // count the number of cpus
            var maxCpus = topCpus.Max(x => x.TopCpu.percentages.Length);
            var maxTicks = 720; // ticks per hour. 1 tick = 5 seconds

            var cpuObjects = new CpuDataModel
            {
                cpus = new Cpu[maxCpus]
            };
            // initialize the cpu collectors
            for (var index = 0; index < maxCpus; index++)
            {
                cpuObjects.cpus[index] = new Cpu { Ticks = new int[maxTicks] };
            }

            foreach (var topCpu in topCpus)
            {
                var baseHour = (int)topCpu.Timestamp.Date.AddHours(hour).TimeOfDay.TotalSeconds;
                var secondsAfterMidnight = (int)topCpu.Timestamp.TimeOfDay.TotalSeconds;
                var secondsIntoHour = secondsAfterMidnight - baseHour;
                var startTick = secondsIntoHour / 5;

                var processes = topCpu.TopCpu.processes;
                foreach (var process in processes)
                {
                    cpuObjects.Processes.Add(new Process
                    {
                        Name = process.pname,
                        X = startTick,
                        Y = process.cpuPercentage
                    });
                    if (process.cpuPercentage != 0)
                    {

                    }
                }

                for (var index = 0; index < topCpu.TopCpu.percentages.Length; index++)
                {
                    var percentages = topCpu.TopCpu.percentages[index];
                    var t = 0;
                    foreach (var tick in percentages.cpuPercentages)
                    {
                        if (startTick + t >= 0 && startTick + t < maxTicks)
                        {
                            cpuObjects.cpus[index].Ticks[startTick + t] = tick;
                        }
                        t++;
                    }
                }
            }
            return cpuObjects;
        }

        public class TopCpu
        {
            public DateTime snapshot;
            public CpuPercentageTO[] percentages;
            public List<ProcessTO> processes = new List<ProcessTO>();
        }

        public class CpuPercentageTO
        {
            public int[] cpuPercentages;
        }

        public class ProcessTO
        {
            public int pid;
            public string pname;
            public int cpuPercentage;
        }


        private IEnumerable<CpuStuff> GetDataForDay(int itemId, DateTime startDate)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                return (from d in
                            scope.Get<CpuUsage>().Where(
                                d => d.Timestamp.Date == startDate && d.Item.ItemId == itemId)
                        orderby d.Timestamp
                        select new CpuStuff
                        {
                            Timestamp = d.Timestamp,
                            JsonData = d.JsonData
                        }).ToList();
            }
        }
    
    private IEnumerable<CpuStuff> GetDataForHour(int itemId, DateTime startDate, int hour)
    {
        var startTime = startDate.AddHours(hour);
        var endTime = startDate.AddHours(hour + 1);
        using (var scope = _dbContextScopeFactory.Create())
        {
            return (from d in
                        scope.Get<CpuUsage>().Where(
                            d => d.Timestamp >= startTime && d.Timestamp < endTime  && d.Item.ItemId == itemId)
                    orderby d.Timestamp
                    select new CpuStuff
                    {
                        Timestamp = d.Timestamp,
                        JsonData = d.JsonData
                    }).ToList();
        }
    }
}

    public class Cpu
    {
        public int[] Ticks;
    }

    public class Process
    {
        public string Name;
        public int X;
        public int Y;
    }

    public class CpuDataModel
    {
        public Cpu[] cpus;
        public List<Process> Processes = new List<Process>();
    }

    public class CpuStuff
    {
        public DateTime Timestamp;
        public string JsonData;
    }
}
