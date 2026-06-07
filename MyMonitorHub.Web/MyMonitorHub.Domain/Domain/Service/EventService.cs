using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MyMonitorHub.Common.WebApi;
using MyMonitorHub.Domain.BO;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using Microsoft.Extensions.Logging;
using Configuration = MyMonitorHub.Domain.Entity.Configuration;
using Event = MyMonitorHub.Domain.Entity.Event;

namespace MyMonitorHub.Domain.Service
{
    public class EventService
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IDbContextScopeFactory _contextScopeFactory;
        public EventService(IDbContextScopeFactory contextScopeFactory, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<EventService>();
            _contextScopeFactory = contextScopeFactory;
        }


        public List<Event> GetEventsForSr(int id)
        {
            using (var scope = _contextScopeFactory.Create())
            {
                return scope.Get<Event>().Where(x => x.ServiceRequestId == id).ToList();
            }
        }

        private class AccountInfo
        {
            public int AccountId { get; set; }
            public int? HealthResponseAlertMinutes { get; set; }
        }

        public void FindTardy()
        {
            _logger.LogDebug("Start of EventService.FindTardy() method");
            var now = DateTime.Now;
            List<AccountInfo> accounts;
            using (var scope = _contextScopeFactory.Create())
            {
                accounts = (scope.Get<Configuration>()
                    .Select(x => new AccountInfo
                    {
                        AccountId = x.accountId,
                        HealthResponseAlertMinutes = x.healthResponseAlertMinutes
                    })).ToList();
            } // using

            foreach (var account in accounts)
            {
                // look for absent responses
                if (account.HealthResponseAlertMinutes != null)
                {
                    List<TempItemGroupedById> devices;
                    // New as of 30-Jan-2010 - ignore records that are not marked as constantly reporting in.
                    var account3 = account;

                    using (var scope = _contextScopeFactory.Create())
                    {

                        devices =
                            (from i in
                                scope.Get<Item>().Where(
                                    i =>
                                        i.AccountId == account3.AccountId &&
                                        i.TimeStamp <= now.AddMinutes(-(double)(account3.HealthResponseAlertMinutes ?? 0)) && i.ConstantlyReportsInYN)
                                select new TempItem
                                {
                                    DeviceId = i.DeviceId,
                                    CatName = i.Category.Description,
                                    SubCatName = i.SubCategoryName,
                                    Item = i
                                }
                                into g
                                group g by g.DeviceId
                                into gg
                                select new TempItemGroupedById
                                {
                                    DeviceId = gg.Key,
                                    DataItems = gg
                                }).ToList();
                    } // scope

                    foreach (var device in devices)
                    {
                        using (var scope = _contextScopeFactory.Create())
                        {
                            var processor = new EventProcessor(account.AccountId, device.DeviceId,
                                _contextScopeFactory, _loggerFactory);


                            foreach (var itemData in device.DataItems)
                            {
                                var item = itemData.Item;

                                scope.Attach(item); // attach this item to the scope

                                var evt = new Common.WebApi.Event
                                {
                                    Category = itemData.CatName,
                                    // not needed. Item already exists - crap they are needed for Event row
                                    ItemName = item.Description, // not needed. Item already exists
                                    SubCategory = itemData.SubCatName, // not needed. Item already exists
                                    Status = EventType.Fail,
                                    StatusDescription = EventProcessor.AbsentReponse,
                                    Timestamp = DateTime.Now
                                };

                                processor.ProcessItem(item, evt);
                            } // foreach itemData

                            scope.SaveChanges();
                        } // scope
                    } // foreach group
                } // (account.healthResponseAlertMinutes != null)
            } // foreach account
        } // findTardy

        private class TempItem
        {
            public int DeviceId { get; set; }
            public string CatName { get; set; }
            public string SubCatName { get; set; }
            public Item Item { get; set; }
        }

        private class TempItemGroupedById
        {
            public int DeviceId { get; set; }
            public IGrouping<int,TempItem> DataItems { get; set; }
        }

        private bool TimeBetween(DateTime datetime, TimeSpan start, TimeSpan end)
        {
            // convert datetime to a TimeSpan
            var now = datetime.TimeOfDay;
            // see if start comes before end
            if (start < end)
                return start <= now && now <= end;
            // start is after end, so do the inverse comparison
            return !(end < now && now < start);
        }

        private void StripSomeEvents(int deviceId, Common.WebApi.EventPacket packet)
        {
            var keeperEvents = new List<Common.WebApi.Event>();
            var stripEvents = new List<Common.WebApi.Event>();
            foreach (var evnt in packet.Events)
            {
                if (evnt.Status == EventType.Ok) continue;

                var removeIt = false;
                //-----------------------------------------------------------------

                switch (evnt.Category)
                {
                    case "HttpVortex":
                    {
                        if (evnt.StatusDescription.Contains("The operation has timed out") ||
                            evnt.StatusDescription.Contains("Exceeds"))
                        {
                            removeIt = true;
                        }

                        break;
                    }
                    case "Errors":
                    {
                        if (evnt.StatusDescription.Contains("Check Failed"))
                        {
                            removeIt = true;
                        }

                        break;
                    }
                }

                // handle ones between 9pm and 7am then
                if (TimeBetween(DateTime.Now, new TimeSpan(21, 0, 0), new TimeSpan(07, 0, 0)))
                {
                    // put something here if you want to ignore it within this time range
                }

                //-----------------------------------------------------------------

                if (!removeIt)
                {
                    keeperEvents.Add(evnt);
                }
                else
                {
                    stripEvents.Add(evnt);
                }
            }

            if (stripEvents.Count > 0)
            {
                packet.Events = keeperEvents.ToArray();
                foreach (var evnt in stripEvents)
                {
                    _logger.LogDebug($"    Stripping event: {evnt.Category}|{evnt.SubCategory}|{evnt.ItemName}|{evnt.StatusDescription}");
                }
            }

        }

        public void WriteEvents(int accountId, int deviceId, Common.WebApi.EventPacket packet, bool startTardy = true)
        {
            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug) && packet.Events.Any(x => x.Status != EventType.Ok))
            {
                using (var scope = _contextScopeFactory.Create())
                {
                    var names = scope.Get<Device>().Where(x => x.DeviceId == deviceId)
                        .Select(x => new {DeviceName = x.Description, GroupName = x.DeviceGroup.Description}).FirstOrDefault();
                    _logger.LogDebug("{0} > {1}", names?.GroupName, names?.DeviceName);
                }
            }

            StripSomeEvents(deviceId, packet);

            if (_logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug) && packet.Events.Any(x => x.Status != EventType.Ok))
            {
                foreach (var evnt in packet.Events)
                {
                    _logger.LogDebug(
                        $"    failed evt: category = {evnt.Category}, subcat = {evnt.SubCategory}, item = {evnt.ItemName}, status = {evnt.Status}, desc = {evnt.StatusDescription}");
                }
            }

            if (packet?.Events == null)
            {
                throw new NoLogException("Packet is not in a valid format");
            }

            Account acct;
            DeviceInfo device;

            using (var scope = _contextScopeFactory.Create())
            {
                acct = scope.Get<Account>().FirstOrDefault(x => x.AccountId == accountId);
                device = scope.Get<Device>().Where(x => x.AccountId == accountId &&
                    x.DeviceId == deviceId && x.Deleted == false)
                    .Select(x => new DeviceInfo
                    {
                        DeviceId = x.DeviceId,
                        PauseUntilDateTime = x.PauseUntilDateTime,
                        AccountName = acct.Description,
                        GroupName = x.DeviceGroup.Description,
                        DeviceName = x.Description
                    })
                    .FirstOrDefault();
            }

            if (acct == null || device == null)
            {
                throw new NoLogException("Either the account ID or device ID is invalid");
            }

            if (startTardy) TardyThread.Current.Start(); // if the thread died then this will restart it


            // make sure all the categories are created ahead of time
            using (var scope = _contextScopeFactory.Create())
            {
                foreach (var evt in packet.Events)
                {
                    new CategoryService(_contextScopeFactory).GetOrCreateCatName(accountId, evt.Category);
                }
                scope.SaveChanges();
            }

            using (var scope = _contextScopeFactory.Create())
            {
                // created once per device
                var processor = new EventProcessor(accountId, deviceId, _contextScopeFactory, _loggerFactory);

                // multiple events can come in from one device.
                foreach (var evt in packet.Events)
                {
                    // if the itemId is zero or invalid then try to create the item first

                    if (evt.Category == "Disk" && evt.SubCategory == "All")
                    {
                        evt.SubCategory = "Usage";
                    }

                    var item = new ItemService(_contextScopeFactory).Get(accountId, deviceId, evt.Category, evt.SubCategory,
                            evt.ItemName, evt.Style);

                    if (item == null)
                    {
                        // will have been already created above
                        var cat = new CategoryService(_contextScopeFactory).GetOrCreateCatName(accountId, evt.Category);

                        // new Item. We need to persist it now.
                        using (var scope2 = _contextScopeFactory.Create(DbContextOption.CreateNew))
                        {

                            item = new Item
                            {
                                Description = evt.ItemName,
                                DeviceId = device.DeviceId,
                                Status = (int)EventType.Unknown,
                                StatusDescription = "",
                                SubCategoryName = evt.SubCategory,
                                AccountId = accountId,
                                CategoryId = cat.CategoryId,
                                TimeStamp = DateTime.Now,
                                ConstantlyReportsInYN = ItemService.DoesSendHealth(evt.Style)
                            };
                            scope2.Add(item);
                            scope2.SaveChanges();
                            scope.Attach(item);
                        }
                    }
                    else
                    {
                        // item exists
                        if (device.PauseUntilDateTime != null && device.PauseUntilDateTime > DateTime.Now)
                        {
                            // this item belongs to a device that is currently paused(example software update)
                            if (item.CategoryId == 1001 || item.CategoryId == 1027)
                            {
                                // at this time we only care about Process and CPU
                                evt.Status = (EventType)item.Status; // don't change the status during a pause
                                evt.StatusDescription = "Paused until: " + device.PauseUntilDateTime.Value.ToString("HH:mm");
                            }
                        }

                    }

                    processor.ProcessItem(item, evt);
                } // foreach
                scope.SaveChanges();
            } // using
        } // write events

        private class DeviceInfo
        {
            public int DeviceId;
            public DateTime? PauseUntilDateTime;
            public string AccountName;
            public string GroupName;
            public string DeviceName;
        }

        /// <summary>
        /// Validates an API Key against the database. Used only by AgentTokenService at the token endpoint.
        /// All other agent requests authenticate via JWT access token.
        /// </summary>
        public ConnectValidationResult ConnectionValidation(int accountId, int deviceId, string apiKey)
        {
            _logger.LogDebug("ConnectionValidation({0},{1})", accountId, deviceId);

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new NoLogException("API Key is required");

            Account acct;
            DeviceInfo? device = null;

            using (var scope = _contextScopeFactory.Create())
            {
                acct = scope.Get<Account>().FirstOrDefault(x => x.AccountId == accountId);
                if (acct != null)
                    device = scope.Get<Device>().Where(x => x.AccountId == accountId &&
                                                            x.DeviceId == deviceId && x.Deleted == false &&
                                                            x.ApiKey == apiKey)
                        .Select(x => new DeviceInfo
                        {
                            DeviceId = x.DeviceId,
                            PauseUntilDateTime = x.PauseUntilDateTime,
                            AccountName = acct.Description,
                            GroupName = x.DeviceGroup.Description,
                            DeviceName = x.Description
                        })
                        .FirstOrDefault();
            }

            if (acct == null || device == null)
                throw new NoLogException("Either the account ID, device ID, or API Key is invalid");

            return new ConnectValidationResult
            {
                AccountName = device.AccountName,
                GroupName = device.GroupName,
                DeviceName = device.DeviceName
            };
        }

        /// <summary>
        /// Validates that a device exists and belongs to the account (no credential check — used after JWT auth).
        /// </summary>
        public ConnectValidationResult? DeviceValidation(int accountId, int deviceId)
        {
            _logger.LogDebug("DeviceValidation({0},{1})", accountId, deviceId);

            using (var scope = _contextScopeFactory.Create())
            {
                var acct = scope.Get<Account>().FirstOrDefault(x => x.AccountId == accountId);
                if (acct == null) return null;

                var device = scope.Get<Device>().Where(x => x.AccountId == accountId &&
                                                            x.DeviceId == deviceId && x.Deleted == false)
                    .Select(x => new DeviceInfo
                    {
                        DeviceId = x.DeviceId,
                        AccountName = acct.Description,
                        GroupName = x.DeviceGroup.Description,
                        DeviceName = x.Description
                    })
                    .FirstOrDefault();

                if (device == null) return null;

                return new ConnectValidationResult
                {
                    AccountName = device.AccountName,
                    GroupName = device.GroupName,
                    DeviceName = device.DeviceName
                };
            }
        }
    } // class

    public class NoLogException : Exception
    {
        public NoLogException(string message) : base(message)
        {
        }
    }
}