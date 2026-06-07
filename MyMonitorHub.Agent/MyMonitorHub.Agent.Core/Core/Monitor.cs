using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MyMonitorHub.Common.WebApi;
using MyMonitorHub.Agent.Common;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MyMonitorHub.Agent.Core
{
    /// <summary>
    ///     This is the main start up program for the client monitor server.
    /// </summary>
    public class Monitor
    {
        private static int _port;

        private static readonly EventQueue EventQueue = new EventQueue();

        private static readonly ILogger Logger = Log.ForContext<Monitor>();
        private static readonly Dictionary<string, ItemData> Items = new Dictionary<string, ItemData>();
        private readonly List<IThreadShutdown> _threads = new List<IThreadShutdown>();
        private MonitorHubClient? _monitorHubClient;
        private static ProcessCollectionThread? _processCollector;

        // all writes to the disk must use this lock to ensure a forced exist on a different thread will not
        // interupt a write. Doesn't include Serilog and I don't care about those. I care about monitor.json mainly.
        public static readonly object FileWriteLock = new object();

        // main

        public static int Port => _port;

        internal static List<Event> EventList => EventQueue.EventList;

        public static ProcessCollectionThread? ProcessCollector
        {
            get { return _processCollector; }
            set { _processCollector = value; }
        }

        public void Run()
        {
            try
            {
                RunTrapped();
            }
            catch (Exception e)
            {
                Logger.Error($"{e.Message}\r\n{e.StackTrace}");
            }
        }

        private void RunTrapped()
        {

            _monitorHubClient = new MonitorHubClient(MonitorProfile.Current.AccountId, MonitorProfile.Current.DeviceId);
            _monitorHubClient.Start();

            _port = MonitorProfile.Current.CommandPort;

            var config = ReadConfig();
            if (config["version"] == null)
            {
                throw new Exception("Error in appsettings.json, expecting 'version' field");
            }

            // start up the event reporting thread
            // wakes up every minute and reports on what it finds in the event queue
            // will not report to the server if the event queue is empty
            var eventReporter = new EventReportingThread();
            _threads.Add(eventReporter);
            eventReporter.Start();

            // this process runs every 5 seconds and does a quick collection of processes and
            // their current cpu usage
            _processCollector = new ProcessCollectionThread();
            _threads.Add(_processCollector);
            _processCollector.Start();

            // start up the thread that listens for events
            var eventListener = new CommandListenerThread(_port);
            _threads.Add(eventListener);
            eventListener.Start();

            // wait 15 seconds to give the event lister thread time to start up
            // because one of the next threads is the command watcher plugin with expects it is running.
            Thread.Sleep(15000);

            // now lets fire up all the client plugins
            var pluginArray = config["plugins"] as JArray;

            if (pluginArray != null)
            {
                var length = pluginArray.Count;
                for (var index = 0; index < length; index++)
                {
                    var pluginThread = new PluginThread(index, false);
                    _threads.Add(pluginThread);
                    pluginThread.Start();
                }
            }

            // The new model supports multiple plugins in one thread
            // each group plugin is one thread
            var groupPluginArray = config["group-plugins"] as JArray;
            if (groupPluginArray != null)
            {
                var length = groupPluginArray.Count;
                for (var index = 0; index < length; index++)
                {
                    var groupPluginThread = new GroupPluginThread(index);
                    groupPluginThread.Start();
                    _threads.Add(groupPluginThread);
                }
            }
        }


        internal static JObject ReadConfig()
        {
            lock (typeof(Monitor))
            {
                return JObject.Parse(File.ReadAllText(MonitorProfile.MonitorFilePath));
            }
        }

        internal static bool FireEvent(string category, string subCategory, string itemName, string description,
            EventType status,
            ItemStyle style, DateTime currentRunTime)
        {
            if (style == ItemStyle.NoHealth)
            {
                var evt = new Event
                {
                    Category = category,
                    SubCategory = subCategory,
                    ItemName = itemName,
                    StatusDescription = description,
                    Status = status,
                    Style = style,
                    Timestamp = DateTime.Now
                };

                EventQueue.Add(evt);
                return true;
            }
            var key = category + "|" + subCategory + "|" + itemName;
            var itemData = GetItemData(key);
            var overdue = CheckOverdue(currentRunTime, itemData);
            if (itemData == null || status != itemData.LastStatus || overdue)
            {
                SetItemData(key, new ItemData {LastStatus = status, LastSent = currentRunTime});
                var evt = new Event
                {
                    Category = category,
                    SubCategory = subCategory,
                    ItemName = itemName,
                    StatusDescription = description,
                    Status = status,
                    Style = style,
                    Timestamp = DateTime.Now
                };


                EventQueue.Add(evt);
                return true;
            }
            return false;
        }

        private static void SetItemData(string key, ItemData itemData)
        {
            lock (Items)
            {
                Items[key] = itemData;
            }
        }

        private static ItemData? GetItemData(string key)
        {
            try
            {
                lock (Items)
                {
                    return Items[key];
                }
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        private static bool CheckOverdue(DateTime currentRunTime, ItemData? itemData)
        {
            if (itemData == null) return true;

            var millis = (currentRunTime - itemData.LastSent).TotalMilliseconds;
            var minutes = (int) (millis/1000)/60;
            // TODO should not hardcode this
            return minutes >= 10; // ReportMinutes
        }

        internal void Stop()
        {
            _monitorHubClient?.Stop();

            foreach (var thread in _threads)
            {
                thread.Stop();
            }
        }

        private class ItemData
        {
            public EventType LastStatus { get; set; }
            public DateTime LastSent { get; set; }
        }
    }

    //class
}