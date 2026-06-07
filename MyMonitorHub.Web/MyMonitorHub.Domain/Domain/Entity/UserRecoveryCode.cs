using System;

namespace MyMonitorHub.Domain.Entity
{
    public class UserRecoveryCode
    {
        public int UserRecoveryCodeId { get; set; }
        public int UserId { get; set; }
        public string CodeHash { get; set; } = string.Empty;
        public DateTime? UsedAt { get; set; }
        public virtual User User { get; set; } = null!;
    }
}
