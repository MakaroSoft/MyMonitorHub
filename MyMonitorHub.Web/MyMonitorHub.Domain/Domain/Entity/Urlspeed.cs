namespace MyMonitorHub.Domain.Entity
{
    public partial class UrlSpeed
    {
        public int UrlSpeedId { get; set; }
        public int ItemId { get; set; }
        public System.DateTime Timestamp { get; set; }
        public int CurSpeedMS { get; set; }
        public int MinSpeedMS { get; set; }
        public int MaxSpeedMS { get; set; }
        public int AvgSpeedMS { get; set; }
        public virtual Item Item { get; set; } = null!;
    }
}
