namespace MyMonitorHub.Domain.Entity
{
    public partial class Error
    {
        public int ErrorId { get; set; }
        public int ItemId { get; set; }
        public System.DateTime Timestamp { get; set; }
        public int NewErrorCount { get; set; }
        public virtual Item Item { get; set; } = null!;
    }
}
