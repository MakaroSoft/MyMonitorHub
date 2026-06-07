using System.IO;
using MyMonitorHub.Common.WebApi;
using MyMonitorHub.Agent.Core;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MyMonitorHub.Agent.Plugins
{
    public class DiskSpaceMonitor : AbstractPlugin
    {
        private static readonly ILogger Logger = Log.ForContext<DiskSpaceMonitor>();

        private string _category;
        private int _count;
        private Disk[] _disks;

        protected override void Execute()
        {
            if (_disks == null)
            {
                _category = Dom.GetAttribute(PluginElement["category"], "name");

                var disksArray = PluginElement["disks"] as JArray;
                if (disksArray != null)
                {
                    _count = disksArray.Count;
                    _disks = new Disk[_count];
                    for (var ind = 0; ind < _count; ind++)
                    {
                        var disk = new Disk();
                        _disks[ind] = disk;
                        var item = disksArray[ind];
                        disk.Name = Dom.GetAttribute(item, "name");
                        disk.FailThresholdMb = int.Parse(Dom.GetAttribute(item, "threshold-mb"));
                        disk.WarnThresholdMb = int.Parse(Dom.GetAttribute(item, "warn-threshold-mb", "0"));
                        disk.SubCategory = Dom.GetAttribute(item, "sub-category");
                        disk.Stats = new Stats();
                        Logger.Information("    " + Title + " - Registering disk name: " + disk.Name);
                    }
                }
            }

            for (var index = 0; index < _count; index++)
            {
                if (_disks == null) break; // should never happen

                var disk = _disks[index];
                var totalMb = GetTotalMegaBytes(disk.Name);
                var freeMb = GetFreeMegaBytes(disk.Name);
                var usedMb = totalMb - freeMb;

                var currentStats = disk.Stats;

                currentStats.Count++;
                currentStats.Cur = usedMb;

                if (currentStats.Count == 1)
                {
                    currentStats.Min = usedMb;
                    currentStats.Max = usedMb;
                    currentStats.Avg = usedMb;
                    currentStats.Total = usedMb;
                }
                else
                {
                    if (usedMb < currentStats.Min)
                    {
                        currentStats.Min = usedMb;
                    }
                    if (usedMb > currentStats.Max)
                    {
                        currentStats.Max = usedMb;
                    }
                    currentStats.Total += usedMb;
                    currentStats.Avg = currentStats.Total/currentStats.Count;
                }

                var desc = format(totalMb, currentStats, disk.FailThresholdMb, disk.WarnThresholdMb);

                EventType theStatus;
                if (freeMb < disk.FailThresholdMb)
                {
                    theStatus = EventType.Fail;
                }
                else
                {
                    theStatus = EventType.Ok;
                }

                // remember we poll every 2 minutes but don't always send back a response. only if first time, 
                // state has changed(ok/fail), or statistics collection(every 10 minutes)
                if (SayStatus(_category, disk.SubCategory, disk.Name, desc, theStatus))
                {
                    // we sent it to the server so reset poll count
                    currentStats.Count = 0;
                }
            }
        }

        private string format(int totalMb, Stats currentStats, int failThresholdMb, int warnThresholdMb)
        {
            var formatted = "_@DISK@_|" + totalMb + "|" + currentStats.Cur + "|" + currentStats.Min + "|" +
                               currentStats.Max + "|" + currentStats.Avg + "|" + failThresholdMb + "|" + warnThresholdMb;
            return formatted;
        }

        private static string StripColon(string diskName)
        {
            return diskName.EndsWith(":") ? diskName.Substring(0, diskName.Length - 1) : diskName;
        }

        private int GetFreeMegaBytes(string diskName)
        {
            var driveInfo = new DriveInfo(StripColon(diskName));
            return (int)(driveInfo.TotalFreeSpace / (1024 * 1024));
        }
        private int GetTotalMegaBytes(string diskName)
        {
            var driveInfo = new DriveInfo(StripColon(diskName));
            return (int)(driveInfo.TotalSize/(1024*1024));
        }

        public class Disk
        {
            public int FailThresholdMb { get; set; }
            public string Name { get; set; }
            public Stats Stats { get; set; }
            public string SubCategory { get; set; }
            public int WarnThresholdMb { get; set; }
        }

        public class Stats
        {
            public int Avg { get; set; }
            public int Count { get; set; }

            public int Cur { get; set; }
            public int Max { get; set; }
            public int Min { get; set; }
            public int Total { get; set; }
        }
    }
}