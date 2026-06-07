using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Exceptions;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Models;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Util;
using Microsoft.Extensions.Logging;

namespace MyMonitorHub.Domain.Service
{
    public class ContactService
    {
        // ReSharper disable once UnusedMember.Local
        private readonly ILogger _logger;

        private readonly IDbContextScopeFactory _dbContextScopeFactory;

        public ContactService(IDbContextScopeFactory dbContextScopeFactory, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ContactService>();
            _dbContextScopeFactory = dbContextScopeFactory;
        }

        public ContactDetailModel GetDetail(int id, int deviceGroupId)
        {

            if (id == -1)
            {
                string deviceGroupDescription;
                using (var scope = _dbContextScopeFactory.Create())
                {
                    var info =
                        scope.Get<DeviceGroup>()
                            .Where(x => x.DeviceGroupId == deviceGroupId)
                            .Select(x => new {x.Description, x.AccountId})
                            .FirstOrDefault();
                    if (info == null)
                    {
                        throw new Exception("Device group not found: " + deviceGroupId);
                    }
                    if (info.AccountId != Helper.AccountId)
                    {
                        throw new Exception("You do not have permission to access the device group: " + deviceGroupId);
                    }
                    deviceGroupDescription = info.Description;

                    return new ContactDetailModel
                    {
                        ContactId = -1,
                        DeviceGroupId = deviceGroupId,
                        DeviceGroupDescription = deviceGroupDescription,
                        Notes = "",
                        Name = "",
                        Phone1 = "",
                        PhoneType1Name = "",
                        Title = "<new>"
                    };
                }

            }



            ContactDetailModel device;
            using (var scope = _dbContextScopeFactory.Create())
            {
                device = scope.Get<Contact>().Where(x => x.ContactId == id).ProjectToContactDetailModel()
                    .FirstOrDefault();
                if (device == null)
                {
                    throw new Exception($"Device with the id of - {id} - was not found");
                }

            }
            device.Title = device.Name;

            return device;
        }

        public int Insert(ContactDetailModel model)
        {
            var name = model.Name.Trim();
            if (name.Length < 3)
            {
                throw new Exception("Contact name must be at least 3 characters");
            }
            using (var scope = _dbContextScopeFactory.Create())
            {
                var c =
                    scope.Get<Contact>()
                        .FirstOrDefault(x => x.DeviceGroupId == model.DeviceGroupId && x.Name == name);
                if (c != null)
                {
                    throw new Exception("Contact already exists");
                }
                var dg = scope.Get<DeviceGroup>().FirstOrDefault(x => x.DeviceGroupId == model.DeviceGroupId);
                if (dg == null)
                {
                    throw new Exception("Device group does not exist: " + model.DeviceGroupId);
                }
                if (dg.AccountId != Helper.AccountId)
                {
                    throw new Exception("You do not have permission to create a Contact on this device group.");
                }

                // update the fields
                var contact = new Contact()
                {
                    Name = name,
                    DeviceGroupId = model.DeviceGroupId,
                    Notes = model.Notes,
                    Phone1 = model.Phone1,
                    Phone2 = model.Phone2,
                    Phone3 = model.Phone3,
                    PhoneType1Id = model.PhoneType1Id,
                    PhoneType2Id = model.PhoneType2Id,
                    PhoneType3Id = model.PhoneType3Id
                };

                if (string.IsNullOrEmpty(contact.Phone2)) contact.PhoneType2Id = null;
                if (string.IsNullOrEmpty(contact.Phone3)) contact.PhoneType3Id = null;

                scope.Add(contact);
                scope.SaveChanges();
                return contact.ContactId;
            }
        }

        public void Update(ContactDetailModel model)
        {
            model.Name = model.Name.Trim();
            if (model.Name.Length < 3)
            {
                throw new Exception("Contact name must be at least 3 characters");
            }
            using (var scope = _dbContextScopeFactory.Create())
            {
                var contact = scope
                    .Get<Contact>().FirstOrDefault(d => d.ContactId == model.ContactId);
                if (contact == null)
                {
                    throw new Exception("Contact not found: " + model.ContactId);
                }

                if (contact.Name != model.Name)
                {
                    var count =
                        scope.Get<Contact>()
                            .Count(x => x.DeviceGroupId == model.DeviceGroupId && x.Name == model.Name);
                    if (count != 0)
                    {
                        throw new Exception("Contact name already exists");
                    }
                }
                // update the fields
                contact.Name = model.Name;
                contact.Notes = model.Notes;

                contact.Phone1 = model.Phone1;
                contact.Phone2 = model.Phone2;
                contact.Phone3 = model.Phone3;

                contact.PhoneType1Id = model.PhoneType1Id;
                contact.PhoneType2Id = model.PhoneType2Id;
                contact.PhoneType3Id = model.PhoneType3Id;

                if (string.IsNullOrEmpty(contact.Phone2)) contact.PhoneType2Id = null;
                if (string.IsNullOrEmpty(contact.Phone3)) contact.PhoneType3Id = null;

                scope.SaveChanges();
            }

        }

        public void Delete(int accountId, int id)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var contact =
                    scope.Get<Contact>().FirstOrDefault(x => x.ContactId == id);
                if (contact == null)
                {
                    throw new Exception("Contact not found - " + id);
                }
                if (contact.DeviceGroup.AccountId != accountId)
                {
                    throw new Exception("Permission denied to delete contact - " + id);
                }

                scope.Delete(contact);
                scope.SaveChanges();
            }
        }

        public List<ContactModel> GetContacts(int deviceGroupId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                if (!Authorizer.IsAdministrator)
                {
                    var dgAccountId = scope.Get<DeviceGroup>()
                        .Where(x => x.DeviceGroupId == deviceGroupId)
                        .Select(x => x.AccountId)
                        .FirstOrDefault();
                    if (dgAccountId != Helper.AccountId)
                        throw new SecurityException("You only have permission to access your own account contacts.");
                }

                var phoneTypes = scope.Get<PhoneType>().ToList();

                var result =
                    scope.Get<Contact>()
                        .Where(x => x.DeviceGroupId == deviceGroupId)
                        .ProjectToContactModel()
                        .ToList();
                foreach (var c in result)
                {
                    c.PhoneType1Name = GetPhoneType(phoneTypes, c.PhoneType1Id);
                    c.PhoneType2Name = GetPhoneType(phoneTypes, c.PhoneType2Id);
                    c.PhoneType3Name = GetPhoneType(phoneTypes, c.PhoneType3Id);
                }
                return result;
            }
        }

        private string GetPhoneType(List<PhoneType> list, int? id)
        {
            if (id == null)
            {
                return "";
            }
            return list.Where(x => x.PhoneTypeId == id).Select(x => x.Name).FirstOrDefault();
        }
    }
}
