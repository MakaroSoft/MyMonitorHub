namespace MyMonitorHub.Domain.Models
{
    public class DeviceModel
    {
        public int DeviceId { get; set; }
        public string? ApiKey { get; set; }
        public Nullable<int> DeviceGroupId { get; set; }
        public string Description { get; set; }
        public int ChildIndex { get; set; }
        public int AccountId { get; set; }
        public Nullable<int> DeviceTypeId { get; set; }
        public bool Deleted { get; set; }
        public string WebUrl { get; set; }

        public string Notes { get; set; }
    }
}