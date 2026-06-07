using System;
using System.Collections.Generic;
using System.Linq;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyMonitorHub.Domain.Service
{
    public class MonthlyReportService
    {

        private readonly IDbContextScopeFactory _dbContextScopeFactory;
        private readonly ILogoService _logoService;
        private readonly ILoggerFactory _loggerFactory;

        public MonthlyReportService(IDbContextScopeFactory dbContextScopeFactory, ILogoService logoService, ILoggerFactory loggerFactory = null)
        {
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _dbContextScopeFactory = dbContextScopeFactory;
            _logoService = logoService;
        }

        public MonthlyReportService(IDbContextScopeFactory dbContextScopeFactory, ILoggerFactory loggerFactory = null)
        {
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _dbContextScopeFactory = dbContextScopeFactory;
        }

        public List<int> AllEmailed(int accountId, int year, int month)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                return scope.Get<MonthlyReport>()
                    .Where(x => x.AccountId == accountId && x.Year == year && x.Month == month)
                    .Select(x => x.DeviceGroupId)
                    .ToList();
            }
        }

        public void Insert(MonthlyReport r)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                scope.Add(r);
                scope.SaveChanges();
            }
        }

        public bool Exists(int deviceGroupId, int year, int month)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var result =
                    scope.Get<MonthlyReport>().FirstOrDefault(x =>
                        x.DeviceGroupId == deviceGroupId && x.Year == year && x.Month == month);
                return result != null;
            }
        }



        public MonthlyReportModel GetMonthlyReportModel(int deviceGroupId, int year, int month, int accountId)
        {
            // Verify the device group belongs to the caller's account before loading any data.
            using (var scope = _dbContextScopeFactory.Create())
            {
                var groupExists = scope.Get<DeviceGroup>()
                    .Any(x => x.DeviceGroupId == deviceGroupId && x.AccountId == accountId);
                if (!groupExists)
                    throw new Exception("Device group not found or does not belong to your account.");
            }

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var d = new DeviceService(_dbContextScopeFactory, _loggerFactory);
            var devices = d.GetDevicesForGroup(deviceGroupId);

            // collect the devices
            var total = 0;
            var deviceModels = new List<DeviceModel>();
            foreach (var device in devices)
            {
                var serviceRequests = new ServiceRequestService(_dbContextScopeFactory, _loggerFactory).GetServiceRequests(
                    device.DeviceId,
                    startDate, endDate);

                total += serviceRequests.Count;

                var serviceRequestModels = serviceRequests.Select(x => new ServiceRequestModel
                {
                    Description = x.Notes,
                    EventTime = x.TimeStamp,
                    Id = x.ServiceRequestId,
                    Events = ToEventModel(x.Events)
                }).ToList();

                // a count of errors for each itemId
                var errorCounts = GetErrors(device.DeviceId, startDate, endDate);

                var deviceModel = new DeviceModel
                {
                    Name = device.Description,
                    ServiceRequests = serviceRequestModels,
                    DiskImages = GetDiskImages(device.DeviceId, year, month),
                    Errors = GetErrors(device.DeviceId, errorCounts)
                };
                deviceModels.Add(deviceModel);
            }


            var myModel = new MonthlyReportModel
            {
                Logo = _logoService.GetLogo(),
                CustomerName = new DeviceGroupService(_dbContextScopeFactory).GetCompanyName(deviceGroupId),
                ReportDate = startDate.ToString("MMMM yyyy"),
                TotalServiceRequests = total,
                ServiceRequestsByDay = GetServiceRequestsByDay(deviceModels, year, month),
                WebServerImage = GetWebServiceImage(deviceGroupId, year, month),
                Devices = deviceModels
            };

            return myModel;
        }

        private CalendarGrid[,] GetServiceRequestsByDay(List<DeviceModel> deviceModels, int year, int month)
        {
            // order the service requests by day
            var dayHasSr = new bool[31];

            foreach (var device in deviceModels)
            {
                foreach (var sr in device.ServiceRequests)
                {
                    var day = sr.EventTime.Day - 1;
                    dayHasSr[day] = true;
                }
            }

            var firstDay = new DateTime(year, month, 1);
            var dow = firstDay.DayOfWeek;

            var lastDay = firstDay.AddMonths(1).AddDays(-1).Day;

            var p = 1 - (int)dow;

            var serviceRequestsByDay = new CalendarGrid[6, 7];
            for (var y = 0; y < 6; y++)
            {
                for (var x = 0; x < 7; x++)
                {
                    var calendarGrid = new CalendarGrid
                    {
                        Day = "",
                        HasSr = false
                    };
                    if (p >= 1 && p <= lastDay)
                    {
                        calendarGrid.Day = p.ToString();
                        if (dayHasSr[p - 1])
                        {
                            calendarGrid.HasSr = true;
                        }
                    }

                    serviceRequestsByDay[y, x] = calendarGrid;
                    p++;
                }
            }

            return serviceRequestsByDay;
        }

        private IList<EventModel> ToEventModel(ICollection<Event> events)
        {
            return events.Select(ev => new EventModel
            {
                Description = ev.ItemName,
                EventTime = ev.ServerReceivedTimeStamp,
                Id = ev.EventId,
                Item = new ItemModel
                {
                    Name = ev.ItemName,
                    Status = ev.Status ?? 1
                }
            }).ToList();
        }

        private string GetWebServiceImage(int deviceGroupId, int year, int month)
        {
            var itemId = new ItemService(_dbContextScopeFactory).GetWebServerId(deviceGroupId);
            if (itemId == 0) return null;


            var weekDate = new DateTime(year, month, 1);

            var stream =
                new ResponseGraphService(_dbContextScopeFactory).RenderGraph(itemId, "monthEnd",
                    weekDate.ToShortDateString());

            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, (int)stream.Length);

            return "data:image/jpeg;base64," + Convert.ToBase64String(bytes);
        }

        private IList<DiskImageModel> GetDiskImages(int deviceId, int year, int month)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var images = scope.Get<Item>()
                    .Where(x => x.DeviceId == deviceId && x.Category.Description == "Disk" &&
                                x.SubCategoryName == "Usage")
                    .Select(x => new DiskImageModel
                    {
                        Id = x.ItemId,
                        Name = x.Description
                    }).ToList();

                foreach (var image in images)
                {
                    image.ImageData = GetDiskImageData(image.Id, year, month);
                }

                return images;
            }
        }

        private string GetDiskImageData(int itemId, int year, int month)
        {
            var date = new DateTime(year, month, 1);

            var stream =
                new DiskGraphService(_dbContextScopeFactory).RenderGraph(itemId, "month", date.ToShortDateString());
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, (int)stream.Length);

            return "data:image/jpeg;base64," + Convert.ToBase64String(bytes);
        }

        private List<ErrorItemModel> GetErrors(int deviceId, List<ErrorCountsPerItem> errorCounts)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var errors = scope.Get<Item>()
                    .Where(x => x.DeviceId == deviceId && x.Category.Description == "Errors" &&
                                x.SubCategoryName == "Disk")
                    .Select(x => new ErrorItemModel
                    {
                        ItemId = x.ItemId,
                        Name = x.Description
                    }).ToList();

                foreach (var error in errors)
                {
                    error.ErrorCount = errorCounts
                        .Where(x => x.ItemId == error.ItemId)
                        .Select(x => x.Count)
                        .FirstOrDefault();
                }

                return errors;
            }
        }

        private List<ErrorCountsPerItem> GetErrors(int deviceId, DateTime startDate, DateTime endDate)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                return
                    (from e in
                            scope.Get<Error>().Where(e => e.Item.DeviceId == deviceId && e.Timestamp >= startDate && e.Timestamp < endDate)
                        group e by e.Item.ItemId
                        into g
                        select new ErrorCountsPerItem
                        {
                            ItemId = g.Key,
                            Count = g.Sum(er => er.NewErrorCount)
                        }).ToList();
            }
        }


        public class DeviceModel
        {
            public string Name { get; set; }
            public IList<ServiceRequestModel> ServiceRequests { get; set; } = new List<ServiceRequestModel>();
            public IList<ErrorItemModel> Errors { get; set; } = new List<ErrorItemModel>();
            public IList<DiskImageModel> DiskImages { get; set; } = new List<DiskImageModel>();
        }

        public class CalendarGrid
        {
            public string Day { get; set; }
            public bool HasSr { get; set; }

        }

        public class MonthlyReportModel
        {
            public string CustomerName { get; set; }
            public string ReportDate { get; set; }
            public string Logo { get; set; }

            public int TotalServiceRequests { get; set; }
            public CalendarGrid[,] ServiceRequestsByDay { get; set; }
            public IList<DeviceModel> Devices { get; set; }
            public string WebServerImage { get; set; }
        }

        public class ServiceRequestModel
        {
            public int Id { get; set; }
            public string Description { get; set; }
            public DateTime EventTime { get; set; }
            public IList<EventModel> Events { get; set; } = new List<EventModel>();
        }

        public class ItemModel
        {
            public string Name { get; set; }
            public int Status { get; set; }
            public string ImageData { get; set; }
        }

        public class EventModel
        {
            public int Id { get; set; }
            public DateTime EventTime { get; set; }
            public string Description { get; set; }
            public ItemModel Item { get; set; }
        }

        public class ErrorItemModel
        {
            public int ItemId { get; set; }
            public string Name { get; set; }
            public int ErrorCount { get; set; }
        }

        public class ErrorCountsPerItem
        {
            public int ItemId { get; set; }
            public int Count { get; set; }
        }
    }

    public class DiskImageModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImageData { get; set; }
    }
}
