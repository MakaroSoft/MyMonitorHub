using System;

namespace MyMonitorHub.Domain.Entity
{
    public partial class Configuration
    {
        public int configurationId { get; set; }
        public int accountId { get; set; }
        public int healthResponseMinutes { get; set; }
        public Nullable<int> healthResponseAlertMinutes { get; set; }
        public Nullable<int> srNotAcceptedAlertMinutes { get; set; }
        public virtual Account Account { get; set; } = null!;
    }
}
