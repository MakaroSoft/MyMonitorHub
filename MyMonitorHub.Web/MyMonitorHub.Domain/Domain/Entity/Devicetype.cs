using System.Collections.Generic;

namespace MyMonitorHub.Domain.Entity
{
    public partial class DeviceType
    {
        public DeviceType()
        {
            this.Devices = new List<Device>();
        }

        public int DeviceTypeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public virtual ICollection<Device> Devices { get; set; }
    }
}
