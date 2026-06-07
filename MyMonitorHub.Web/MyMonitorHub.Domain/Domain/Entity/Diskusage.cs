namespace MyMonitorHub.Domain.Entity
{
    public partial class DiskUsage
    {
        public int DiskUsageId { get; set; }
        public int ItemId { get; set; }
        public System.DateTime Timestamp { get; set; }
        public int DiskSize { get; set; }
        public int CurUsedMB { get; set; }
        public int MinUsedMB { get; set; }
        public int MaxUsedMB { get; set; }
        public int AvgUsedMB { get; set; }
        public virtual Item Item { get; set; } = null!;
    }
}
