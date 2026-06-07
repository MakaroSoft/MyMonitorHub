using System.Collections.Generic;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.Domain.Models
{
    public class DeviceGroupDetailModel
    {
        public string Title { get; set; }
        public string PageDescription { get; set; }
        public int DeviceGroupId { get; set; }
        public int PageId { get; set; }
        public int ChildIndex { get; set; }
        public int AccountId { get; set; }
        public string Description { get; set; }
        public string emailsForClosedSRs { get; set; }
        public string ContactInformation { get; set; }
        public string Notes { get; set; }
        public List<DeviceModel> Devices { get; set; }
        public List<ContactModel> Contacts { get; set; }
    }
}
