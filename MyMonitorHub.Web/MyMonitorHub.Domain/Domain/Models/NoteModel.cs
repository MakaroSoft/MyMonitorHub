using System.Collections.Generic;

namespace MyMonitorHub.Domain.Models
{
    public class NoteModel
    {
        public string Description { get; set; }
        public int DeviceGroupId { get; set; }
        public bool IsEditable { get; set; }
        public List<NoteGroupModel> Groups { get; set; }
    }

    public enum NoteGroupModelState
    {
        Inserted,
        Deleted,
        Changed,
        Same
    }

    public class NoteGroupModel
    {
        public int State { get; set; }
        public int NoteGroupId { get; set; }
        public int TemplateId { get; set; }
        public string Description { get; set; }
        public List<NoteGroupFieldModel> Fields { get; set; }
    }

    public class NoteGroupFieldModel
    {
        public int ValuesForNoteGroupId { get; set; }
        public int State { get; set; }
        public int FieldId { get; set; }
        public string FieldName { get; set; }
        public string Value { get; set; }
    }
}