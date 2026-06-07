namespace MyMonitorHub.Domain.Entity
{
    public partial class MonthlyReport
    {
        public int MonthlyReportId { get; set; }
        public int AccountId { get; set; }
        public int DeviceGroupId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
    }
}
