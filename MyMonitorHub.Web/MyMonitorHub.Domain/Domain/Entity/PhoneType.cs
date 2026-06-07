using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyMonitorHub.Domain.Entity
{
    [Table("PhoneType")]
    public partial class PhoneType
    {
        public PhoneType()
        {
            Contacts1 = new HashSet<Contact>();
            Contacts2 = new HashSet<Contact>();
            Contacts3 = new HashSet<Contact>();
        }

        public int PhoneTypeId { get; set; }

        [Required]
        [StringLength(10)]
        public string Name { get; set; } = string.Empty;

        public virtual ICollection<Contact> Contacts1 { get; set; }
        public virtual ICollection<Contact> Contacts2 { get; set; }
        public virtual ICollection<Contact> Contacts3 { get; set; }
    }
}
