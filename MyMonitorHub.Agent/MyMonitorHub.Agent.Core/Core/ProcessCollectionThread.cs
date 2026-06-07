using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Serilog;

namespace MyMonitorHub.Agent.Core
{
    public class ProcessAndCpu
    {
        public int Pid;
        public string Name = string.Empty;

        public float CpuPercentage;
    }

    public class ProcessCollectionThread : IThreadShutdown
    {
        private static readonly ILogger Logger = Log.ForContext<ProcessAndCpu>();

        private CpuCollector? _collector;
        private readonly object _lockObject = new object();

        private volatile bool _stopped = true;
        private readonly object _padlock = new object();

        private List<ProcessAndCpu>? _processesAndCpu;
        private DateTime _lastTime = DateTime.Now;


        public void Run()
        {
            Logger.Information("Starting the process collection thread");

            var cpuCount = Environment.ProcessorCount;
            _collector = new CpuCollector();
            _processesAndCpu = new List<ProcessAndCpu>();

            var perfCountersForProcesses = new PerformanceCounter[cpuCount];


            for (var index = 0; index <= cpuCount - 1; index++)
            {
                try
                {
                    perfCountersForProcesses[index] = new PerformanceCounter("Processor", "% Processor Time",
                        index.ToString(CultureInfo.InvariantCulture));
                    perfCountersForProcesses[index].NextValue();
                }
                catch (Exception e)
                {
                    Logger.Error(e.GetType().FullName + "|" +  e.Message);
                }
            }

            try
            {
                _stopped = false;
                while (!_stopped)
                {
                    var processes = Process.GetProcesses();

                    var counters = new List<PerformanceCounter>();

                    foreach (var process in processes)
                    {
                        try
                        {
                            var counter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName);
                            counter.NextValue();
                            counters.Add(counter);
                        }
                        catch (InvalidOperationException)
                        {
                            // can happen on certain processes that are short lived
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e.Message + "\r\n" + e.StackTrace);
                        }
                    }

                    // wait 5 seconds
                    Thread.Sleep(5000);

                    var cpus = new float[cpuCount];

                    for (var index = 0; index <= cpuCount - 1; index++)
                    {
                        cpus[index] = 0;
                        var perfCounter = perfCountersForProcesses[index];
                        if (perfCounter != null)
                        {
                            try
                            {
                                cpus[index] = perfCountersForProcesses[index].NextValue();
                            }
                            catch (Exception e)
                            {
                                // shouldn't get an error here
                                Logger.Error(e.Message + "\r\n" + e.StackTrace);
                            }
                        }
                    }

                    lock (_lockObject)
                    {
                        _lastTime = DateTime.Now;
                        _processesAndCpu = new List<ProcessAndCpu>(processes.Length);
                        var i = 0;
                        foreach (var counter in counters)
                        {
                            try
                            {
                                var cpu = counter.NextValue();
                                var process = processes[i];
                                i++;
                                _processesAndCpu.Add(new ProcessAndCpu
                                {
                                    CpuPercentage = cpu,
                                    Pid = process.Id,
                                    Name = process.ProcessName,
                                });
                            }
                            catch (InvalidOperationException)
                            {
                                // can happen on certain processes that are short lived
                            }
                            catch (Exception e)
                            {
                                Logger.Error(e.Message+"\r\n"+e.StackTrace);
                            }
                        }

                        var top10 = (from x in _processesAndCpu
                            orderby x.CpuPercentage descending
                            select x).Take(10).ToList();

                        _collector.Push(cpus, top10);
                    } //lock

                } // while

            }
            catch (Exception e)
            {
                Logger.Error(e.Message + "\r\n" + e.StackTrace);
            }
            Logger.Information("Cpu Collector has come to an end.");
        } // method

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

        public string TopCpu(int take)
        {
            lock (_lockObject)
            {
                var cpuCount = Environment.ProcessorCount;

                var last24 = _collector!.GetLast(24); // two minutes worth

                var cpus = new CpuPercentageTO[cpuCount];
                for (var index = 0; index < cpuCount; index++)
                {
                    cpus[index] = new CpuPercentageTO
                    {
                        cpuPercentages = (from x in last24
                            select (int) Math.Round(x.Cpus![index], 0)).ToArray()
                    };
                }

                // GetLast method use above does have cpu and the top 10 processes however
                // it is possible that the user wants more than 10. for this reason I also store ALL processes
                // for the last check

                // processesAndCput contains all processes not just 10
                var processes = (from x in _processesAndCpu
                                 where x.Name != "Idle" /* && (int)Math.Round(x.CpuPercentage / cpuCount, 0) != 0 */
                    orderby x.CpuPercentage descending
                    select new ProcessTO
                    {
                        cpuPercentage = (float)Math.Round(x.CpuPercentage/cpuCount,1),
                        pid = x.Pid,
                        pname = x.Name
                    }).Take(take).ToList();

                var result = new TopCpu
                {
                    snapshot = _lastTime.ToString("MMM d, yyyy hh:mm:ss tt"),
                    percentages = cpus,
                    processes = processes
                };
                return JsonConvert.SerializeObject(result);
            }
        }
    }

    // ReSharper disable InconsistentNaming
    public class TopCpu
    {
        public string? snapshot;
        public CpuPercentageTO[]? percentages;
        public List<ProcessTO> processes = new List<ProcessTO>();
    }
    public class CpuPercentageTO
    {
        public int[]? cpuPercentages;
    }
    public class ProcessTO
    {
        public int pid;
        public string? pname;
        public float cpuPercentage;
    }
    // ReSharper restore InconsistentNaming

    public class CollectorData
    {
        public float[]? Cpus;
        public List<ProcessAndCpu>? Top10;
    }

    public class CpuCollector
    {

        // each entry is 5 seconds so 6 entries would give us the cpu percentage over 30 seconds
        private const int MaxEntries = 120; // 10 minutes worth

        private readonly List<CollectorData> _entries = new List<CollectorData>();

        public void Push(float[] cpus, List<ProcessAndCpu> top10)
        {
            _entries.Add(new CollectorData
            {
                Cpus = cpus,
                Top10 = top10
            });
            if (_entries.Count == MaxEntries + 1)
            {
                _entries.RemoveAt(0);
            }
        }

        //use 24 for 2 minutes worth
        public CollectorData[] GetLast(int take)
        {
            // take the last 24 entries. pad front with zeros if less than 24

            var result = new CollectorData[take];

            var start = 0;
            var offset = _entries.Count - take;
            if (offset < 0)
            {
                start = Math.Abs(offset);
            }

            var cpuCount = Environment.ProcessorCount;

            for (var index = 0; index <= start - 1; index++)
            {
                result[index] = new CollectorData
                {
                    Cpus = new float[cpuCount],
                    Top10 = new List<ProcessAndCpu>()
                };
            }
            for (var index = start; index < take; index++)
            {
                result[index] = _entries[index + offset];
            }

            return result;
        }

    }
}
