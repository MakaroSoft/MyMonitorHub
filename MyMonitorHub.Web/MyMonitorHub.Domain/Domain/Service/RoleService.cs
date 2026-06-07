using System.Collections.Generic;
using System.Linq;
using System.Xml;
using MyMonitorHub.Domain.Dto;
using MyMonitorHub.Domain.Exceptions;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Util;
using Role = MyMonitorHub.Domain.Entity.Role;

namespace MyMonitorHub.Domain.Service
{
    /// <summary>
    ///     Summary description for Pages
    /// </summary>
    public class RoleService
    {
        private readonly IDbContextScopeFactory _dbContextScopeFactory;

        public RoleService(IDbContextScopeFactory dbContextScopeFactory)
        {
            _dbContextScopeFactory = dbContextScopeFactory;
        }


        public class RoleMaintData
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public List<PermissionData> Permissions { get; set; }
        }
        public class PermissionData
        {
            public bool Check { get; set; }
            public string GroupCode { get; set; }
            public string PermissionCode { get; set; }
        }

        public RoleMaintData Get(int roleId)
        {
            var permissions = new List<PermissionData>();

            var data = GetRoleModel(roleId);
            if (data == null)
                throw new SecurityException("Role not found or does not belong to your account.");

            var allowedDoc = new XmlDocument();
            allowedDoc.LoadXml(data.RoleXML);

            var p = Permissions.Current;
            foreach (var group in p.Groups)
            {
                foreach (var permission in group.Permissions)
                {
                    var perm = new PermissionData
                    {
                        Check = IsChecked(allowedDoc,group, permission),
                        GroupCode = group.Name,
                        PermissionCode = permission.Name
                    };
                    permissions.Add(perm);
                }
            }

            return new RoleMaintData
            {
                Name = data.RoleCode,
                Description = data.Description,
                Permissions = permissions
            };
        }

        private bool IsChecked(XmlDocument allowedDoc, PermissionGroup group, Permission permission)
        {
            var node =
                allowedDoc.SelectSingleNode("/pages/page[@code='" + group.Name + "']/access[@code='" + permission.Name +
                                            "']");
            return node != null;
        }

        public void Update(int id, RoleMaintData json)
        {
            InsertUpdate(id,json);
        }

        public int InsertUpdate(int id, RoleMaintData json)
        {
            // get only checked ones
            var checkedPermissions = from p in json.Permissions
                                     where p.Check
                                     select p;


            const string docString = @"<?xml version='1.0'?><pages></pages>";

            var doc = new XmlDocument();
            doc.LoadXml(docString);

            var pagesNode = doc.SelectSingleNode("/pages");

            foreach (var permission in checkedPermissions)
            {
                // create the page element if it doesn't exist already
                var pageNode = pagesNode?.SelectSingleNode("page[@code='" + permission.GroupCode + "']");
                if (pageNode == null)
                {
                    pageNode = doc.CreateElement("page");
                    var attr = doc.CreateAttribute("code");
                    attr.Value = permission.GroupCode;
                    pageNode.Attributes?.Append(attr);
                    pagesNode?.AppendChild(pageNode);
                }
                // create the access element
                var accessNode = doc.CreateElement("access");
                var attr2 = doc.CreateAttribute("code");
                attr2.Value = permission.PermissionCode;
                accessNode.Attributes.Append(attr2);

                pageNode.AppendChild(accessNode);
            }
            var xml = doc.OuterXml;

            if (id == 0)
            {
                var role = new Role
                {
                    AccountId = Helper.AccountId,
                    RoleCode = json.Name,
                    RoleXML = xml,
                    Description = json.Description
                };
                Insert(role);
                return role.RoleId;
            }
            Update(id, json.Description, xml);
            return 0;
        }


        public int Insert(RoleMaintData json)
        {
            return InsertUpdate(0, json);
        }


        public void Insert(Role role)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                scope.Add(role);
                scope.SaveChanges();
            }
        }

        private void Update(int roleId, string description, string roleXml)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var orig = scope.Get<Role>().FirstOrDefault(x => x.RoleId == roleId && x.AccountId == Helper.AccountId);
                if (orig == null)
                    throw new SecurityException("Role not found or does not belong to your account.");

                orig.Description = description;
                orig.RoleXML = roleXml;
                scope.SaveChanges();
            }
        }

        public List<RoleListModel> GetSystemRoles()
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                return scope.Get<Role>().Where(x => x.AccountId == null && x.RoleCode != "Administrator")
                    .Select(x => new RoleListModel
                    {
                        RoleId = x.RoleId,
                        Description = x.Description,
                        RoleCode = x.RoleCode,
                        UserCount = x.Members.Count(m => m.Account.AccountId == Helper.AccountId)
                    }).ToList();
            }
        }

        public List<RoleListModel> GetCustomRoles(int accountId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                return scope.Get<Role>().Where(x => x.AccountId == accountId)
                    .Select(x => new RoleListModel
                    {
                        RoleId = x.RoleId,
                        Description = x.Description,
                        RoleCode = x.RoleCode,
                        UserCount = x.Members.Count(m => m.Account.AccountId == Helper.AccountId)
                    }).ToList();
            }
        }

        public void Delete(int id)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var role = scope.Get<Role>().FirstOrDefault(x => x.RoleId == id && x.AccountId == Helper.AccountId);
                if (role == null)
                    throw new SecurityException("Role not found or does not belong to your account.");

                scope.Delete(role);
                scope.SaveChanges();
            }
        }

        // -------------------------------------------------------------

        public RoleModel GetRoleModel(int roleId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                return scope.Get<Role>()
                    .Where(x => x.RoleId == roleId && (x.AccountId == Helper.AccountId || x.AccountId == null))
                    .ProjectToRoleModel()
                    .FirstOrDefault();
            }
        }



    } // class
}