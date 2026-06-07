using System;

namespace MyMonitorHub.Domain.Models
{
    public class ServiceRequestView
    {
        public int ServiceRequestId { get; set; }
        public string DeviceDesc { get; set; }
        public string UserName { get; set; }
        public string Notes { get; set; }
        public DateTime TimeStamp { get; set; }
        public string DeviceGroupDesc { get; set; }
        public int PageId { get; set; }
        public int DeviceGroupId { get; set; }
    }
} // namespace