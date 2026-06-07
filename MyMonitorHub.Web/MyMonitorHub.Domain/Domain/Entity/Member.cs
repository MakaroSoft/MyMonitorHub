using System;

namespace MyMonitorHub.Domain.Entity
{
    public partial class Member
    {
        public int MemberId { get; set; }
        public int OrganizationId { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }

        [Obsolete("SendAlertsTo is deprecated")]
        public string? SendAlertsTo { get; set; }

        public virtual Account Account { get; set; } = null!;
        public virtual Role Role { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public bool DefaultOrganization { get; set; }
    }
}
