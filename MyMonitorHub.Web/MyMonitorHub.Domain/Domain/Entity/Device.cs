using System.Collections.Generic;

namespace MyMonitorHub.Domain.Entity
{
    public partial class Device
    {
        public Device()
        {
            Items = new List<Item>();
            ServiceRequests = new List<ServiceRequest>();
        }

        public int DeviceId { get; set; }
        public string? ApiKey { get; set; }
        public int? DeviceGroupId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int ChildIndex { get; set; }
        public int AccountId { get; set; }
        public int? DeviceTypeId { get; set; }
        public bool Deleted { get; set; }
        public virtual DeviceGroup? DeviceGroup { get; set; }
        public virtual DeviceType? DeviceType { get; set; }
        public virtual ICollection<Item> Items { get; set; }
        public virtual ICollection<ServiceRequest> ServiceRequests { get; set; }
        public string? WebUrl { get; set; }
        public string? Notes { get; set; }
        public string? ContactInformation { get; set; }
        public DateTime? PauseUntilDateTime { get; set; }
        public string? ConfigXml { get; set; }
    }
}
