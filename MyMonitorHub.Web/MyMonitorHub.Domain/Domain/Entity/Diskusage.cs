namespace MyMonitorHub.Domain.Entity
{
    public partial class DiskUsage
    {
        public int DiskUsageId { get; set; }
        public int ItemId { get; set; }
        public System.DateTime Timestamp { get; set; }
        public long DiskSize { get; set; }
        public long CurUsedMB { get; set; }
        public long MinUsedMB { get; set; }
        public long MaxUsedMB { get; set; }
        public long AvgUsedMB { get; set; }
        public virtual Item Item { get; set; } = null!;
    }
}
