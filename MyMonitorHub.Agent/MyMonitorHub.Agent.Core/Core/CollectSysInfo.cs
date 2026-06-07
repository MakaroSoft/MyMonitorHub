using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using System.Management;
using Serilog;

namespace MyMonitorHub.Agent.Core
{
    public class CollectSysInfo
    {
        private static readonly ILogger Logger = Log.ForContext<CollectSysInfo>();

        public string Collect()
        {
            return JsonConvert.SerializeObject(CollectData());
        }

        private SysInfoDTO CollectData()
        {
            var tempurature = "";
            try
            {
                var temp = Temperature.Temperatures;
                if (temp.Count > 1)
                {
                    tempurature += "(" + temp.Count + ")";
                }

                if (temp.Count == 1)
                {
                    tempurature = temp[0].CurrentValue.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    var first = true;
                    foreach (var t in temp)
                    {
                        if (!first)
                        {
                            tempurature += "</cr>";
                        }
                        first = false;
                        tempurature += t.InstanceName + ": " + t.CurrentValue;
                    }
                }
            }
            catch (Exception e)
            {
                tempurature = e.Message;
            }

            var data = new SysInfoDTO
            {
                monitorVersion = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "version.txt"))[0],
                cpus = Environment.ProcessorCount,
                computerType = ComputerType(),
                fans = Fans(),
                osVersion = OsName(),
                powerSupplies = PowerSupplies(),
                temperature = tempurature,
                upTime = UpTime().ToString(@"d\ hh\:mm\:ss")
            };
            return data;
        }

        private TimeSpan UpTime()
        {
            try
            {
                using (var uptime = new PerformanceCounter("System", "System Up Time"))
                {
                    uptime.NextValue(); //Call this an extra time before reading its value
                    return TimeSpan.FromSeconds(uptime.NextValue());
                }
            }
            catch (Exception e)
            {
                Logger.Error("UpTime|" + e.GetType().FullName + "|" + e.Message);
            }
            return new TimeSpan(0,0,0,0);
        }

        private NameValue[] Fans()
        {
            var supplies = new List<NameValue>();
            try
            {
                //initialize the select query with command text 
                var query = new SelectQuery(@"Select * from Win32_Fan");
                //initialize the searcher with the query it is supposed to execute 
                using (var searcher = new ManagementObjectSearcher(query))
                {
                    //execute the query 
                    foreach (var o in searcher.Get())
                    {
                        var process = (ManagementObject)o;
                        //print system info 
                        process.Get();
                        var state = (string)process["Status"];// + "|" + process["Availability"] + "|" +
                        //process["StatusInfo"] + "|"+process["Caption"];
                        var name = (string)process["Name"];
                        supplies.Add(new NameValue
                        {
                            name = name,
                            value = state
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("Fans|" + e.GetType().FullName + "|" + e.Message);                
            }
            return supplies.ToArray();
        } // methoid

        private NameValue[] PowerSupplies()
        {
           var supplies = new List<NameValue>();
            try
            {
                //initialize the select query with command text 
                var query = new SelectQuery(@"Select * from Win32_ComputerSystem");
                //initialize the searcher with the query it is supposed to execute 
                using (var searcher = new ManagementObjectSearcher(query))
                {
                    int index = 1;
                    //execute the query 
                    foreach (var o in searcher.Get())
                    {
                        var process = (ManagementObject)o;
                        //print system info 
                        process.Get();
                        var state = (UInt16)process["PowerSupplyState"];
                        string stateString;
                        switch (state)
                        {
                            case 1:
                                stateString = "Other";
                                break;
                            case 2:
                                stateString = "Unknown";
                                break;
                            case 3:
                                stateString = "Safe";
                                break;
                            case 4:
                                stateString = "Warning";
                                break;
                            case 5:
                                stateString = "Critical";
                                break;
                            case 6:
                                stateString = "NonRecoverable";
                                break;
                            default:
                                stateString = state.ToString(CultureInfo.InvariantCulture);
                                break;
                        }
                        supplies.Add(new NameValue
                        {
                            name = "Supply" + index,
                            value = stateString
                        });
                        index++;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("PowerSupplies|" + e.GetType().FullName + "|" + e.Message);
            }
            return supplies.ToArray();
        } // methoid

        private string ComputerType()
        {
            try
            {
                //initialize the select query with command text 
                var query = new SelectQuery(@"Select * from Win32_ComputerSystem");
                //initialize the searcher with the query it is supposed to execute 
                using (var searcher = new ManagementObjectSearcher(query))
                {
                    //execute the query 
                    foreach (var o in searcher.Get())
                    {
                        var process = (ManagementObject)o;
                        //print system info 
                        process.Get();
                        return process["Manufacturer"] + ", " + process["Model"];
                    }
                }                
            }
            catch (Exception e)
            {
                Logger.Error("ComputerType|" + e.GetType().FullName + "|" + e.Message);
            }

            return "";
        } // methoid

        private string OsName()
        {
            try
            {
                //initialize the select query with command text 
                var query = new SelectQuery(@"Select * from Win32_OperatingSystem");
                //initialize the searcher with the query it is supposed to execute 
                using (var searcher = new ManagementObjectSearcher(query))
                {
                    //execute the query 
                    foreach (var o in searcher.Get())
                    {
                        var process = (ManagementObject)o;
                        //print system info 
                        process.Get();
                        var name = (string)process["Name"];
                        var index = name.IndexOf("|", StringComparison.Ordinal);
                        return index != -1 ? name.Substring(0, index) : name;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("OsName|" + e.GetType().FullName + "|" + e.Message);
            }

            return "";
        } // methoid
    }



    public class Temperature
    {
        public double CurrentValue { get; private set; }
        public string? InstanceName { get; private set; }
        public static List<Temperature> Temperatures
        {
            get
            {
                var result = new List<Temperature>();
                var searcher = new ManagementObjectSearcher(@"root\WMI",
                    "SELECT * FROM MSAcpi_ThermalZoneTemperature");
                foreach (var o in searcher.Get())
                {
                    var obj = (ManagementObject) o;
                    var temp = Convert.ToDouble(obj["CurrentTemperature"].ToString());
                    temp = (temp - 2732)/10.0;
                    result.Add(new Temperature {CurrentValue = temp, InstanceName = obj["InstanceName"]?.ToString()});
                }
                return result;
            }
        }
    }




    // ReSharper disable InconsistentNaming
    public class SysInfoDTO
    {
        public string? computerType;
        public string? osVersion;
        public int cpus;
        public string? upTime;
        public string? monitorVersion;

        public string? temperature;
        public NameValue[]? powerSupplies;
        public NameValue[]? fans;
        
    }
    public class NameValue
    {
        public string? name;
        public string? value;
    }
    // ReSharper restore InconsistentNaming
}
