using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Exceptions;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Models;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Util;

namespace MyMonitorHub.Domain.Service
{
    public class NotesService
    {
        public enum Mode
        {
            View = 1,
            Edit = 2
        }

        private readonly IDbContextScopeFactory _contextScopeFactory;
        public NotesService(IDbContextScopeFactory contextScopeFactory)
        {
            _contextScopeFactory = contextScopeFactory;
        }


        public List<System.Collections.Generic.KeyValuePair<string, string>> GetExistingGroups()
        {
            using (var scope = _contextScopeFactory.Create())
            {
                var existingGroups =
                    (from ng in scope.Get<NoteGroup>().Where(x => x.DeviceGroup.AccountId == Helper.AccountId)
                        select new
                        {
                            ng.NoteGroupTemplateId,
                            ng.Description
                        }).Distinct().ToList();
                var list = new List<System.Collections.Generic.KeyValuePair<string, string>>();
                foreach (var eg in existingGroups)
                {
                    list.Add(new System.Collections.Generic.KeyValuePair<string, string>(eg.NoteGroupTemplateId.ToString(CultureInfo.InvariantCulture), eg.Description));
                }
                return list;
            }
        }

        public List<System.Collections.Generic.KeyValuePair<string, string>> GetTemplates()
        {
            using (var scope = _contextScopeFactory.Create())
            {
                var templates = (from ngt in scope.Get<NoteGroupTemplate>().Where(x => x.AccountId == Helper.AccountId)
                    select new
                    {
                        ngt.NoteGroupTemplateId,
                        ngt.Description
                    }).ToList();

                var list = new List<System.Collections.Generic.KeyValuePair<string, string>>();
                foreach (var t in templates)
                {
                    list.Add(new System.Collections.Generic.KeyValuePair<string, string>(t.NoteGroupTemplateId.ToString(CultureInfo.InvariantCulture), t.Description));
                }
                return list;
            }
        }

        public NoteGroupModel GetNoteGroupModel(int templateId, string description)
        {
            using (var scope = _contextScopeFactory.Create())
            {
                // TODO why is this here?
                //scope.Get<NoteGroupTemplate>().Where(x => x.NoteGroupTemplateId == templateId).ToList();

                var group = new NoteGroupModel();
                group.Description = description;
                group.NoteGroupId = -1;
                group.TemplateId = templateId;
                group.State = (int) NoteGroupModelState.Inserted;
                var fieldsList = new List<NoteGroupFieldModel>();
                group.Fields = fieldsList;

                CollectFields(templateId, fieldsList);
                return group;
            } // using
        }

        public NoteModel GetNoteModel(int deviceGroupId, Mode mode)
        {
            var model = new NoteModel();
            var groups = new List<NoteGroupModel>();
            model.Groups = groups;

            using (var scope = _contextScopeFactory.Create())
            {
                if (!Authorizer.IsAdministrator)
                {
                    var securityInfo =
                        scope.Get<DeviceGroup>().Where(x => x.DeviceGroupId == deviceGroupId).Select(x => new
                        {
                            accountId = x.AccountId,
                            deviceGroupId = x.DeviceGroupId,
                            pageId = x.PageId
                        }).FirstOrDefault();
                    if (securityInfo == null)
                    {
                        throw new Exception("Invalid data.");
                    }

                    if (securityInfo.accountId != Helper.AccountId)
                    {
                        throw new SecurityException("You only have permission to access your own account pages.");
                    }

                }
                model.DeviceGroupId = deviceGroupId;
                model.IsEditable = Authorizer.Authorize(Permissions.CanEditNotes);

                model.Description =
                    scope.Get<DeviceGroup>().Where(x => x.DeviceGroupId == deviceGroupId)
                        .Select(x => x.Description)
                        .FirstOrDefault();
                var noteGroups =
                    scope.Get<NoteGroup>().Where(x => x.DeviceGroupId == deviceGroupId).ToList();
                foreach (var noteGroup in noteGroups)
                {
                    var group = new NoteGroupModel();
                    groups.Add(group);
                    group.Description = noteGroup.Description;
                    group.NoteGroupId = noteGroup.NoteGroupId;
                    group.TemplateId = noteGroup.NoteGroupTemplateId;
                    group.State = (int) NoteGroupModelState.Same;
                    var fieldsList = new List<NoteGroupFieldModel>();
                    group.Fields = fieldsList;

                    CollectFields(noteGroup.NoteGroupTemplateId, fieldsList);

                    var data =
                        scope.Get<ValuesForNoteGroup>().Where(x => x.NoteGroupId == noteGroup.NoteGroupId)
                            .Select(
                                x =>
                                    new
                                    {
                                        fieldId = x.FieldId,
                                        value = x.Value,
                                        valuesForNoteGroupId = x.ValuesForNoteGroupId
                                    });

                    foreach (var fields in data)
                    {
                        addValue(fieldsList, fields.fieldId, fields.value, fields.valuesForNoteGroupId);
                    }
                    if (mode == Mode.View)
                    {
                        removeBlanks(fieldsList);
                    }
                }
            } // using
            return model;
        }

        private void CollectFields(int noteGroupTemplateId, List<NoteGroupFieldModel> list)
        {
            using (var scope = _contextScopeFactory.Create())
            {
                var addList = new List<NoteGroupFieldModel>();

                var template =
                    scope.Get<NoteGroupTemplate>().Where(x => x.NoteGroupTemplateId == noteGroupTemplateId)
                        .FirstOrDefault();

                foreach (var fields in template.FieldsForNoteGroupTemplates)
                {
                    addList.Add(new NoteGroupFieldModel
                    {
                        State = (int) NoteGroupModelState.Inserted,
                        ValuesForNoteGroupId = 0, // only required for update
                        FieldId = fields.FieldId,
                        FieldName = fields.Field.FieldName,
                        Value = ""
                    });
                }
                list.InsertRange(0, addList);

                if (template.Inherits != null)
                {
                    CollectFields(template.Inherits.Value, list);
                }
            } // using
        }

        // in display mode we don't want to see any fields that are blank
        private void removeBlanks(List<NoteGroupFieldModel> fieldList)
        {
            var toDelete = new List<NoteGroupFieldModel>();

            foreach (var info in fieldList)
            {
                if (info.Value == null || info.Value.Trim() == "")
                {
                    toDelete.Add(info);
                }
            }
            foreach (var info in toDelete)
            {
                fieldList.Remove(info);
            }
        }

        // adds the value to the collection of fields
        private void addValue(IEnumerable<NoteGroupFieldModel> fields, int id, string value, int valuesForNoteGroupId)
        {
            foreach (var info in fields)
            {
                if (info.FieldId == id)
                {
                    info.ValuesForNoteGroupId = valuesForNoteGroupId;
                    info.State = (int) NoteGroupModelState.Same;
                    info.Value = value;
                    return;
                }
            }
        }

        public void UpdateNotes(int id, List<NoteGroupModel> filtered)
        {
            using (var scope = _contextScopeFactory.Create())
            {
                foreach (var group in filtered)
                {
                    switch (group.State)
                    {
                        case (int) NoteGroupModelState.Inserted:
                            var ng = new NoteGroup
                            {
                                Description = group.Description,
                                DeviceGroupId = id,
                                NoteGroupTemplateId = group.TemplateId
                            };
                            foreach (var field in group.Fields)
                            {
                                if (!string.IsNullOrEmpty(field.Value))
                                {
                                    var vfng = new ValuesForNoteGroup
                                    {
                                        FieldId = field.FieldId,
                                        NoteGroup = ng,
                                        Value = field.Value
                                    };
                                    scope.Add(vfng);
                                }
                            }
                            scope.Add(ng);
                            break;
                        case (int) NoteGroupModelState.Deleted:
                            scope.Delete<NoteGroup>(group.NoteGroupId);
                            break;
                        case (int) NoteGroupModelState.Same:
                        case (int) NoteGroupModelState.Changed:
                            if (group.State == (int) NoteGroupModelState.Changed)
                            {
                                var ng2 = scope.Get<NoteGroup>().FirstOrDefault(x => x.NoteGroupId == @group.NoteGroupId);
                                ng2.Description = group.Description;
                            }
                            // the children may still have  been changed
                            foreach (var field in group.Fields)
                            {
                                switch (field.State)
                                {
                                    case (int) NoteGroupModelState.Inserted:
                                        if (!string.IsNullOrEmpty(field.Value))
                                        {
                                            var vfng = new ValuesForNoteGroup
                                            {
                                                FieldId = field.FieldId,
                                                NoteGroupId = group.NoteGroupId,
                                                Value = field.Value
                                            };
                                            scope.Add(vfng);
                                        }
                                        break;
                                    case (int) NoteGroupModelState.Changed:
                                        if (string.IsNullOrEmpty(field.Value))
                                        {
                                            scope.Delete<ValuesForNoteGroup>(field.ValuesForNoteGroupId);
                                        }
                                        else
                                        {
                                            var vfng2 =
                                                scope.Get<ValuesForNoteGroup>().FirstOrDefault(x => x.ValuesForNoteGroupId == field.ValuesForNoteGroupId);
                                            vfng2.Value = field.Value;
                                        }
                                        break;
                                } // switch
                            }
                            break;
                    } // switch
                } // foreach
                scope.SaveChanges();
            } // using
        }
    } // class
} // namespace