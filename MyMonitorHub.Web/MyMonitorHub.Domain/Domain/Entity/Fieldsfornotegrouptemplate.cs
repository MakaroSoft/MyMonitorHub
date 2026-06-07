namespace MyMonitorHub.Domain.Entity
{
    public partial class FieldsForNoteGroupTemplate
    {
        public int FieldsForNoteGroupId { get; set; }
        public int NoteGroupTemplateId { get; set; }
        public int FieldId { get; set; }
        public virtual Field Field { get; set; } = null!;
        public virtual NoteGroupTemplate NoteGroupTemplate { get; set; } = null!;
    }
}
