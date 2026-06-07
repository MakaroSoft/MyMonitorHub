namespace MyMonitorHub.Domain.Entity
{
    public partial class ValuesForNoteGroup
    {
        public int ValuesForNoteGroupId { get; set; }
        public int NoteGroupId { get; set; }
        public string Value { get; set; } = string.Empty;
        public int FieldId { get; set; }
        public virtual Field Field { get; set; } = null!;
        public virtual NoteGroup NoteGroup { get; set; } = null!;
    }
}
