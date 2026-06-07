using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Exceptions;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Models;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Service;
using MyMonitorHub.Domain.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MyMonitorHub.Web.Controllers
{
    [Authorize]
        public class DeviceGroupController : BaseController
    {
        private readonly IDbContextScopeFactory _contextScopeFactory;

        public DeviceGroupController(IDbContextScopeFactory contextScopeFactory)
            : base(contextScopeFactory)
        {
            _contextScopeFactory = contextScopeFactory;
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            if (!Authorizer.Authorize(Permissions.CanEditDeviceGroupMaint))
                throw new SecurityException(Permissions.CanEditDeviceGroupMaint.FailMessage);
            try
            {
                new DeviceGroupService(_contextScopeFactory).Delete(Helper.AccountId, id);
                return Json(new { Status = "Success", Response = "Successfully deleted device group", DeviceGroupId = id });
            }
            catch (Exception e)
            {
                return Json(new { Status = "Fail", Reason = e.Message });
            }
        }

        [HttpPost]
        public JsonResult Edit([FromBody] DeviceGroupDetailModel model)
        {
            if (!Authorizer.Authorize(Permissions.CanEditDeviceGroupMaint))
                throw new SecurityException(Permissions.CanEditDeviceGroupMaint.FailMessage);
            try
            {
                if (model.DeviceGroupId == -1)
                {
                    var deviceGroupId = new DeviceGroupService(_contextScopeFactory).Insert(model);
                    return Json(new { Status = "Success", Response = "Successfully Created DeviceGroup", DeviceGroupId = deviceGroupId });
                }
                else
                {
                    new DeviceGroupService(_contextScopeFactory).Update(model);
                    return Json(new { Status = "Success", Response = "Successfully Updated Device Group" });
                }
            }
            catch (Exception e)
            {
                return Json(new { Status = "Fail", DeviceName = model.Description, Reason = e.Message });
            }
        }

        public IActionResult Edit(int id = -1, int pageId = -1)
        {
            if (!Authorizer.Authorize(Permissions.CanViewDeviceGroupMaint))
                throw new SecurityException(Permissions.CanViewDeviceGroupMaint.FailMessage);
            var data = new DeviceGroupService(_contextScopeFactory).GetDetail(id, pageId);
            ViewBag.notesMd = MarkdownHelper.ToHtml(data.Notes);
            return View(data);
        }

        [HttpPost]
        public IActionResult Save(int id, string notes, string contact)
        {
            if (!Authorizer.Authorize(Permissions.CanEditNotes))
                throw new SecurityException(Permissions.CanEditNotes.FailMessage);

            using (var scope = _contextScopeFactory.Create(DbContextOption.CreateNew))
            {
                var deviceGroup = scope.Get<DeviceGroup>()
                    .FirstOrDefault(x => x.DeviceGroupId == id && (Authorizer.IsAdministrator || x.AccountId == Helper.AccountId));
                if (deviceGroup != null)
                {
                    deviceGroup.Notes = notes;
                    deviceGroup.ContactInformation = contact;
                    scope.SaveChanges();
                }
            }
            return NotesOnly(id);
        }

        [HttpPost]
        public IActionResult CreateGroup(int id, [StringLength(255)] string description)
        {
            if (!Authorizer.Authorize(Permissions.CanEditDeviceGroupMaint))
                throw new SecurityException(Permissions.CanEditDeviceGroupMaint.FailMessage);
            if (description != null && description.Length > 255)
                return BadRequest("Description must not exceed 255 characters.");
            var model = new NotesService(_contextScopeFactory).GetNoteGroupModel(id, description);
            return Json(model);
        }

        public IActionResult Notes(int id) => View(GetModel(id, NotesService.Mode.View));

        public IActionResult NotesOnly(int id, int deviceId = -1)
        {
            if (!Authorizer.Authorize(Permissions.CanViewNotes))
                throw new SecurityException(Permissions.CanViewNotes.FailMessage);

            using (var scope = _contextScopeFactory.Create(DbContextOption.CreateNew))
            {
                var deviceGroup = scope.Get<DeviceGroup>()
                    .FirstOrDefault(x => x.DeviceGroupId == id && (Authorizer.IsAdministrator || x.AccountId == Helper.AccountId));
                if (deviceGroup == null)
                    return Forbid();

                ViewBag.Notes = MarkdownHelper.ToHtml(deviceGroup.Notes);
                if (deviceId != -1)
                {
                    var deviceNotes = scope.Get<Device>()
                        .Where(x => x.DeviceId == deviceId).Select(x => x.Notes).FirstOrDefault();
                    ViewBag.DeviceNotes = MarkdownHelper.ToHtml(deviceNotes);
                }
                else
                {
                    var deviceNotes = scope.Get<Device>()
                        .Where(x => x.DeviceGroupId == id && !string.IsNullOrEmpty(x.Notes))
                        .Select(x => new NoteInfo { Notes = x.Notes, Description = x.Description })
                        .ToList();
                    foreach (var note in deviceNotes)
                        note.Notes = MarkdownHelper.ToHtml(note.Notes);
                    ViewBag.AllDeviceNotes = deviceNotes;
                }

                ViewBag.Contacts = scope.Get<Contact>()
                    .Where(x => x.DeviceGroupId == id)
                    .ProjectToContactDetailModel().ToList();
            }
            return View(GetModel(id, NotesService.Mode.View));
        }

        public class NoteInfo
        {
            public string? Notes { get; set; }
            public string? Description { get; set; }
        }

        public IActionResult NotesOnlyEdit(int id)
        {
            if (!Authorizer.Authorize(Permissions.CanEditNotes))
                throw new SecurityException(Permissions.CanEditNotes.FailMessage);

            using var scope = _contextScopeFactory.Create(DbContextOption.CreateNew);
            var deviceGroup = scope.Get<DeviceGroup>().FirstOrDefault(x => x.DeviceGroupId == id);
            if (deviceGroup != null)
            {
                ViewBag.Notes = deviceGroup.Notes;
                ViewBag.ContactInformation = deviceGroup.ContactInformation;
                ViewBag.DeviceGroupId = id;
            }
            return View();
        }

        private NoteModel GetModel(int id, NotesService.Mode mode)
        {
            if (!Authorizer.Authorize(Permissions.CanViewNotes))
                throw new SecurityException(Permissions.CanViewNotes.FailMessage);
            var notesService = new NotesService(_contextScopeFactory);
            var note = notesService.GetNoteModel(id, mode);
            if (mode == NotesService.Mode.Edit)
            {
                ViewBag.existingGroups = notesService.GetExistingGroups();
                ViewBag.templates = notesService.GetTemplates();
            }
            return note;
        }
    }
}
