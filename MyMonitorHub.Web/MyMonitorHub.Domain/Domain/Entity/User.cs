using System.Collections.Generic;

namespace MyMonitorHub.Domain.Entity
{
    public partial class User
    {
        public User()
        {
            this.Members = new List<Member>();
            this.ServiceRequests = new List<ServiceRequest>();
            this.EmailNotifications = new List<EmailNotification>();
        }

        public int UserId { get; set; }
        public string? Password { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? HomePhone { get; set; }
        public string? WorkPhone { get; set; }
        public string? CellPhone { get; set; }
        public string? Email { get; set; }
        public string? HomePage { get; set; }
        public string? TwoFactorSecret { get; set; }
        public bool? TwoFactorEnabled { get; set; }
        public virtual ICollection<Member> Members { get; set; }
        public virtual ICollection<ServiceRequest> ServiceRequests { get; set; }
        public virtual ICollection<EmailNotification> EmailNotifications { get; set; }
    }
}
