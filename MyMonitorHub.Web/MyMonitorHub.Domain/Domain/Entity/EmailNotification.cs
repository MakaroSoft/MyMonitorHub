using System;
using System.Collections.Generic;

namespace MyMonitorHub.Domain.Entity
{
    public partial class EmailNotification
    {
        public int EmailNotificationId { get; set; }
        public int UserId { get; set; }
        public string? Email { get; set; }
        public bool? Disabled { get; set; }

        public virtual User User { get; set; } = null!;
    }
}
