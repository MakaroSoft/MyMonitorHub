namespace MyMonitorHub.Domain.Models
{
    public class DeviceGroupModel
    {
        public int DeviceGroupId { get; set; }
        public int PageId { get; set; }
        public int ChildIndex { get; set; }
        public int AccountId { get; set; }
        public string Description { get; set; }
        public string emailsForClosedSRs { get; set; }
    }
}