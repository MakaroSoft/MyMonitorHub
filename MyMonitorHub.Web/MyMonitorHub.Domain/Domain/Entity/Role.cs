using System.Collections.Generic;

namespace MyMonitorHub.Domain.Entity
{
    public partial class Role
    {
        public Role()
        {
            this.Members = new List<Member>();
        }

        public int RoleId { get; set; }
        public int? AccountId { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string? RoleXML { get; set; }
        public string Description { get; set; } = string.Empty;
        public virtual ICollection<Member> Members { get; set; }
    }
}
