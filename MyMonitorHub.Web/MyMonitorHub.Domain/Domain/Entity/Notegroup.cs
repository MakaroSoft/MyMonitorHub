using System.Collections.Generic;

namespace MyMonitorHub.Domain.Entity
{
    public partial class NoteGroup
    {
        public NoteGroup()
        {
            this.ValuesForNoteGroups = new List<ValuesForNoteGroup>();
        }

        public int NoteGroupId { get; set; }
        public int DeviceGroupId { get; set; }
        public int NoteGroupTemplateId { get; set; }
        public string Description { get; set; } = string.Empty;
        public virtual DeviceGroup DeviceGroup { get; set; } = null!;
        public virtual NoteGroupTemplate NoteGroupTemplate { get; set; } = null!;
        public virtual ICollection<ValuesForNoteGroup> ValuesForNoteGroups { get; set; }
    }
}
