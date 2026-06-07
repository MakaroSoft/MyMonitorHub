using System;
using System.Collections.Generic;

namespace MyMonitorHub.Domain.Entity
{
    public partial class NoteGroupTemplate
    {
        public NoteGroupTemplate()
        {
            this.FieldsForNoteGroupTemplates = new List<FieldsForNoteGroupTemplate>();
            this.NoteGroups = new List<NoteGroup>();
        }

        public int NoteGroupTemplateId { get; set; }
        public string Description { get; set; } = string.Empty;
        public Nullable<int> Inherits { get; set; }
        public int AccountId { get; set; }
        public virtual ICollection<FieldsForNoteGroupTemplate> FieldsForNoteGroupTemplates { get; set; }
        public virtual ICollection<NoteGroup> NoteGroups { get; set; }
    }
}
