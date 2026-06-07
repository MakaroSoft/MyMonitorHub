using System.Collections.Generic;

namespace MyMonitorHub.Domain.Entity
{
    public partial class Field
    {
        public Field()
        {
            this.FieldsForNoteGroupTemplates = new List<FieldsForNoteGroupTemplate>();
            this.ValuesForNoteGroups = new List<ValuesForNoteGroup>();
        }

        public int FieldId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public virtual ICollection<FieldsForNoteGroupTemplate> FieldsForNoteGroupTemplates { get; set; }
        public virtual ICollection<ValuesForNoteGroup> ValuesForNoteGroups { get; set; }
    }
}
