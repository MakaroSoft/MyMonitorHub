using System;
using MyMonitorHub.Common.WebApi;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using Microsoft.Extensions.Logging;
using Event = MyMonitorHub.Domain.Entity.Event;

namespace MyMonitorHub.Domain.Service
{
    public class EventProcessor
    {
        public const string AbsentReponse = "Absent response";
        private readonly int _accountId;
        private readonly int _deviceId;
        private readonly ServiceRequestCache _sr;

        private Item _item;
        private ServiceRequest _serviceRequest;
        private readonly ILogger _logger;

        private readonly IDbContextScopeFactory _contextScopeFactory;

        // a new instance of this class is created for each device. Each device works with only one service request
        public EventProcessor(int accountId, int deviceId,
            IDbContextScopeFactory contextScopeFactory, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<EventProcessor>();
            _accountId = accountId;
            _deviceId = deviceId;
            _contextScopeFactory = contextScopeFactory;
            _sr = new ServiceRequestCache(contextScopeFactory, loggerFactory);

            _serviceRequest = null;
        }

        private void HandleDescription(Common.WebApi.Event evt)
        {
            if (evt.StatusDescription.StartsWith("_@DISK@_|"))
            {
                var parms = evt.StatusDescription.Split(new[] {"|"}, StringSplitOptions.None);

                var total = int.Parse(parms[1]);
                var used = int.Parse(parms[2]);
                var min = int.Parse(parms[3]);
                var max = int.Parse(parms[4]);
                var avg = int.Parse(parms[5]);

                var du = new DiskUsage
                {
                    AvgUsedMB = avg,
                    CurUsedMB = used,
                    DiskSize = total,
                    ItemId = _item.ItemId,
                    MaxUsedMB = max,
                    MinUsedMB = min,
                    Timestamp = DateTime.Now
                };
                using (var scope = _contextScopeFactory.Create())
                {
                    scope.Add(du);
                    scope.SaveChanges();
                }
            }
            else if (evt.StatusDescription.StartsWith("_@URL@_|") || evt.StatusDescription.StartsWith("_@URL2@_|"))
            {
                var parms = evt.StatusDescription.Split(new[] {"|"}, StringSplitOptions.None);

                var cur = int.Parse(parms[1]);
                var min = int.Parse(parms[2]);
                var max = int.Parse(parms[3]);
                var avg = int.Parse(parms[4]);

                var us = new UrlSpeed
                {
                    CurSpeedMS = cur,
                    MinSpeedMS = min,
                    MaxSpeedMS = max,
                    AvgSpeedMS = avg,
                    ItemId = _item.ItemId,
                    Timestamp = DateTime.Now
                };
                using (var scope = _contextScopeFactory.Create())
                {
                    scope.Add(us);
                    scope.SaveChanges();
                }
            }
            else if (evt.StatusDescription.StartsWith("_@ERROR@_|"))
            {
                var parms = evt.StatusDescription.Split(new[] {"|"}, StringSplitOptions.None);

                var recent = int.Parse(parms[2]); // recent error count
                if (recent != 0)
                {
                    var er = new Error
                    {
                        ItemId = _item.ItemId,
                        NewErrorCount = recent,
                        Timestamp = DateTime.Now
                    };
                    using (var scope = _contextScopeFactory.Create())
                    {
                        scope.Add(er);
                        scope.SaveChanges();
                    }
                }
            }
            else if (evt.StatusDescription.StartsWith("_@CPU@_|"))
            {
                // TODO write out CPU data
                if (evt.StatusDescription.Length > 8)
                {
                    var status = evt.StatusDescription.Substring(8);
                    var entries = status.Split(new[] {"@|@"}, StringSplitOptions.RemoveEmptyEntries);
                    if (entries.Length > 0)
                    {
                        var backupMinutes = (entries.Length - 1)*-2;
                        var time = evt.Timestamp.AddMinutes(backupMinutes);
                            // each entry represents 2 minutes. There should be 5 entries
                        using (var scope = _contextScopeFactory.Create())
                        {
                            foreach (var entry in entries)
                            {
                                var cpu = new CpuUsage()
                                {
                                    ItemId = _item.ItemId,
                                    JsonData = entry,
                                    Timestamp = time
                                };
                                scope.Add(cpu);
                                time = time.AddMinutes(2);
                            }
                            scope.SaveChanges();
                        }
                    }
                }

                evt.StatusDescription = "OK";
            }
        }

        public void ProcessItem(Item item, Common.WebApi.Event evt)
        {
            _item = item;

            // write out url performance, disk performance etc.
            HandleDescription(evt);

            // get the service request if it exists
            _serviceRequest = _sr.Get(_accountId, _deviceId);

            WriteToEventLog(evt, DateTime.Now);
            UpdateItemFields(evt);
        }

        private void UpdateItemFields(Common.WebApi.Event evt)
        {
            if (evt.StatusDescription != AbsentReponse)
            {
                // We want to keep the original timestamp if an 'absent response'
                _item.TimeStamp = DateTime.Now;
            }

            _item.Status = (int)evt.Status;
            _item.StatusDescription = evt.StatusDescription.Length > 50
                ? evt.StatusDescription.Substring(0, 50)
                : evt.StatusDescription;
            if (_serviceRequest != null) _item.LastServiceRequestId = _serviceRequest.ServiceRequestId;
        }

        private void WriteToEventLog(Common.WebApi.Event evt, DateTime timestamp, bool forceEventWorthy = false)
        {
            // write out to event history
            if (forceEventWorthy || IsEventWorthy(evt))
            {
                _logger.LogDebug("{0} || isEventWorth(evt) - {1}>{2}>{3}>{4}",forceEventWorthy, evt.Category, evt.SubCategory, evt.ItemName, evt.StatusDescription);
                if (_serviceRequest == null)
                {
                    _logger.LogDebug("creating service request - {0}>{1}>{2}>{3}", evt.Category, evt.SubCategory, evt.ItemName, evt.StatusDescription);
                    // I want to open a new service request if one isn't already open
                    _serviceRequest = _sr.GetOrCreate(_accountId, _deviceId);
                }
                var eventRow = new Event
                {
                    AccountId = _accountId,
                    DeviceId = _deviceId,
                    Category = evt.Category,
                    SubCategory = evt.SubCategory,
                    ItemName = evt.ItemName,
                    Status = (int)evt.Status,
                    StatusDescription = evt.StatusDescription,
                    ServerReceivedTimeStamp = timestamp,
                    ClientReceivedTimeStamp = evt.Timestamp,
                    ServiceRequest = _serviceRequest
                };

                using (var scope = _contextScopeFactory.Create())
                {
                    scope.Add(eventRow);                    
                }
            }
        }

        private bool IsEventWorthy(Common.WebApi.Event evt)
        {
            if (evt.Status == EventType.Ok)
            {
                if (_serviceRequest != null && _item.Status != (int)EventType.Ok) // don't write two okays in a row.
                {
                    return true; // write the ok status into the service request
                }
            }
            else
            {
                // change in status
                if ((int)evt.Status != _item.Status) return true;

                // change in status description
                var shortDescription = evt.StatusDescription.Length > 50
                    ? evt.StatusDescription.Substring(0, 50)
                    : evt.StatusDescription;
                if (_item.StatusDescription != shortDescription) return true;

                // change in last service request id
                if (_serviceRequest != null)
                {
                    // for a given sr, we don't want to to have two identical events in a row

                    // here is the one scenareo where a duplicate is event worthy. Remember one sr per device.

                    // say a failure event comes in for device A item A which creates SR 1000. The complication comes in if I close this SR and the item is still in
                    // fail mode(I don't think I should ever do this). Anyways, now another failure event comes in for device A item B which creates SR 1001
                    // next a failure event comes in for device A item A which has the same description as the last one. Normally this is a duplicate and I would want to
                    // ignore it(pass back false) but in this case it really is new to this SR 1001 so it is event worthy(pass back true)
                    if (_item.LastServiceRequestId == null || _item.LastServiceRequestId != _serviceRequest.ServiceRequestId) return true;
                }
                else
                {
                    // failure event with no open SR — always event worthy
                    return true;
                }
            }
            return false;
        }
    } // class
}
