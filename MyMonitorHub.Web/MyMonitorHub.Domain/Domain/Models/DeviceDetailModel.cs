using System.Collections.Generic;

namespace MyMonitorHub.Domain.Models
{
    public class DeviceDetailModel : DeviceModel
    {
        public string Title { get; set; }
        public string DeviceGroupDescription { get; set; }
        public IEnumerable<DeviceLayoutModelItemGroup> ItemGroups { get; set; } = new List<DeviceLayoutModelItemGroup>();
    }
}