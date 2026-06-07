using System;

namespace MyMonitorHub.Domain.Entity
{
    public partial class Event
    {
        public int EventId { get; set; }
        public int AccountId { get; set; }
        public int DeviceId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? SubCategory { get; set; }
        public string? ItemName { get; set; }
        public int? Status { get; set; }
        public string StatusDescription { get; set; } = string.Empty;
        public DateTime ClientReceivedTimeStamp { get; set; }
        public DateTime ServerReceivedTimeStamp { get; set; }
        public int? ServiceRequestId { get; set; }
        public virtual ServiceRequest? ServiceRequest { get; set; }
    }
}
