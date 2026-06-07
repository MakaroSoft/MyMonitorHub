using System.Collections.Generic;

namespace MyMonitorHub.Domain.Models
{
    public class PageDetailModel
    {
        public string Title { get; set; }
        public int PageId { get; set; }
        public string Description { get; set; }

        public List<DeviceGroupModel> DeviceGroups { get; set; } 
    }
}
