namespace MyMonitorHub.Domain.Entity
{
    public partial class CpuUsage
    {
        public int CpuUsageId { get; set; }
        public int ItemId { get; set; }
        public System.DateTime Timestamp { get; set; }
        public string? JsonData { get; set; }
        public virtual Item Item { get; set; } = null!;
    }
}
