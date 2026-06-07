using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using MyMonitorHub.Common.WebApi;
using MyMonitorHub.Agent.Core;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MyMonitorHub.Agent.Plugins
{
    public class HttpVortexMonitor : AbstractPlugin
    {
        private static readonly ILogger Logger = Log.ForContext<HttpVortexMonitor>();

        private string _category;
        private int _count;
        private Url[] _urls;
        private const string Match = "Status: <font size=\"5\" color=\"#008000\">Success</font>";

        private const int HugeTime = 1000 * 60 * 5; // 5 minutes

        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        protected override void Execute()
        {
            if (_urls == null)
            {
                _category = Dom.GetAttribute(PluginElement["category"], "name");

                var urlsArray = PluginElement["urls"] as JArray;

                // initialize the urls
                if (urlsArray != null)
                {
                    _count = urlsArray.Count;
                    _urls = new Url[_count];
                    for (var ind = 0; ind < _count; ind++)
                    {
                        var url = new Url();
                        _urls[ind] = url;

                        var item = urlsArray[ind];
                        url.SubCategory = Dom.GetAttribute(item, "sub-category");
                        url.Location = Dom.GetAttribute(item, "location");
                        url.Name = Dom.GetAttribute(item, "name");
                        url.ThresholdMs = int.Parse(Dom.GetAttribute(item, "threshold-ms", "0"));
                        url.Stats = new Stats();
                        Logger.Information($"    {Title} - Registering url: {url.Location}");
                    }
                }
            }

            // nothing to check
            if (_urls == null) return;

            for (var index = 0; index < _count; index++)
            {
                var url = _urls[index];
                var start = DateTime.Now;

                var status = EventType.Ok;
                var desc = "";
                int millis;
                try
                {
                    using var response = _httpClient.GetAsync(url.Location).GetAwaiter().GetResult();
                    var statusCode = response.StatusCode;
                    var statusDescription = response.ReasonPhrase ?? "";

                    if (statusCode == HttpStatusCode.OK)
                    {
                        // remember how long it took
                        var end = DateTime.Now;
                        millis = (int)(end - start).TotalMilliseconds;

                        desc = $"{millis}ms";
                        status = EventType.Ok;

                        // test for threshold if not zero
                        if (url.ThresholdMs != 0 && millis > url.ThresholdMs)
                        {
                            desc = $"Exceeds {millis}ms";
                            status = EventType.Fail;
                        }
                        else if (url.Location.EndsWith("template=ads.ping/ping.xml", StringComparison.OrdinalIgnoreCase))
                        {
                            // get the body to search it for a match
                            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                            Logger.Debug("code = 200, checking ping.xml for success message");
                            if (body.IndexOf(Match, StringComparison.Ordinal) == -1)
                            {
                                var msg = "Could not find success message!";
                                Logger.Debug(msg);
                                desc = msg;
                                status = EventType.Fail;
                                // important for the report because these will show up as fails
                                millis = HugeTime; // something huge so that even averaged with good pings will remain a high average
                            }
                        }
                    }
                    else
                    {
                        // important for the report because these will show up as fails
                        millis = HugeTime; // something huge so that even averaged with exceeded timeout pings will remain a high average
                        desc = $"({(int)statusCode}) {statusDescription}";
                        status = EventType.Fail;
                    }
                }
                catch (TaskCanceledException)
                {
                    // HttpClient throws TaskCanceledException when the timeout elapses
                    millis = HugeTime;
                    desc = "Request timed out";
                    status = EventType.Fail;
                }
                catch (Exception e)
                {
                    millis = HugeTime; // something huge so that even averaged with exceeded timeout pings will remain a high average
                    status = EventType.Fail;
                    desc = e.Message;
                }

                /*
                 * This http monitor called every two minutes but I don't send a response every two minutes unless the status changes. Otherwise its every 10 minutes for reporting.
                 * This is an effort to keep network traffic down.
                 * all concurrent fails or all concurrent passes are averaged
                 */
                var currentStats = url.Stats;

                if (status != EventType.Ok) currentStats.AllPassed = false;

                currentStats.Count++;
                currentStats.Cur = millis;

                if (currentStats.Count == 1)
                {
                    currentStats.Min = millis;
                    currentStats.Max = millis;
                    currentStats.Avg = millis;
                    currentStats.Total = millis;
                }
                else
                {
                    if (millis < currentStats.Min) currentStats.Min = millis;
                    if (millis > currentStats.Max) currentStats.Max = millis;
                    currentStats.Total += millis;
                    currentStats.Avg = currentStats.Total / currentStats.Count;
                }

                if (currentStats.AllPassed && currentStats.Count > 1)
                {
                    desc = $"Average {currentStats.Avg}ms";
                }
                var message = $"_@URL2@_|{currentStats.Cur}|{currentStats.Min}|{currentStats.Max}|{currentStats.Avg}|{desc}";

                // will send to the server if
                //  1) the first time
                //  2) the status has changed
                //  3) time for a report
                if (SayStatus(_category, url.SubCategory, url.Name, message, status))
                {
                    // occurs when an event is sent to the server
                    // this event was sent to the server so reset the statistics.
                    url.Stats.Reset();
                }

            }
        }

        private class Stats
        {
            public int Cur { get; set; }
            public int Min { get; set; }
            public int Max { get; set; }
            public int Avg { get; set; }
            public int Total { get; set; }
            public int Count { get; set; }
            public bool AllPassed { get; set; } = true;

            public void Reset()
            {
                Cur = 0;
                Min = 0;
                Max = 0;
                Avg = 0;
                Total = 0;
                Count = 0;
                AllPassed = true;
            }
        }

        private class Url
        {
            public string Name { get; set; }
            public string SubCategory { get; set; }
            public int ThresholdMs { get; set; }
            public string Location { get; set; }
            public Stats Stats { get; set; }
        }
    }
}