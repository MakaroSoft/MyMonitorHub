using System.Collections.Generic;

namespace MyMonitorHub.Domain.Entity
{
    public partial class Account
    {
        public Account()
        {
            this.Configurations = new List<Configuration>();
            this.Members = new List<Member>();
        }

        public int AccountId { get; set; }
        public string Identification { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Rules { get; set; }
        public virtual ICollection<Configuration> Configurations { get; set; }
        public virtual ICollection<Member> Members { get; set; }
    }
}
