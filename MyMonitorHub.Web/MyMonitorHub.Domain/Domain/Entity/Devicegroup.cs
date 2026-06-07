using System.Collections.Generic;

namespace MyMonitorHub.Domain.Entity
{
    public partial class DeviceGroup
    {
        public DeviceGroup()
        {
            Devices = new List<Device>();
            NoteGroups = new List<NoteGroup>();
            Contacts = new List<Contact>();
        }

        public int DeviceGroupId { get; set; }
        public int PageId { get; set; }
        public int ChildIndex { get; set; }
        public int AccountId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? emailsForClosedSRs { get; set; }
        public virtual ICollection<Device> Devices { get; set; }
        public virtual Page Page { get; set; } = null!;
        public virtual ICollection<NoteGroup> NoteGroups { get; set; }
        public string? Notes { get; set; }
        public string? ContactInformation { get; set; }
        public virtual ICollection<Contact> Contacts { get; set; }
    }
}
