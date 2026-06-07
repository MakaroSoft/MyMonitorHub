using System;
using System.Collections.Generic;

namespace MyMonitorHub.Domain.Entity
{
    public partial class ServiceRequest
    {
        public ServiceRequest()
        {
            this.Events = new List<Event>();
            this.Items = new List<Item>();
        }

        public int ServiceRequestId { get; set; }
        public int AccountId { get; set; }
        public int DeviceId { get; set; }
        public string Notes { get; set; } = string.Empty;
        public Nullable<int> AssignedToId { get; set; }
        public System.DateTime TimeStamp { get; set; }
        public string Status { get; set; } = string.Empty;
        public System.DateTime? LastAlertTime { get; set; }
        public virtual Device Device { get; set; } = null!;
        public virtual ICollection<Event> Events { get; set; }
        public virtual ICollection<Item> Items { get; set; }
        public virtual User? User { get; set; }
    }
}
