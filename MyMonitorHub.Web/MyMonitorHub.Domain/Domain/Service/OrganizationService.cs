using System;
using System.Collections.Generic;
using System.Linq;
using MyMonitorHub.Domain.Util;
using MyMonitorHub.Domain.Interface;
using Member = MyMonitorHub.Domain.Entity.Member;

namespace MyMonitorHub.Domain.Service
{
    public class OrganizationService
    {
        private readonly IDbContextScopeFactory _dbDbContextScopeFactory;

        public OrganizationService(IDbContextScopeFactory dbDbContextScopeFactory)
        {
            _dbDbContextScopeFactory = dbDbContextScopeFactory;
        }


        public class OrganizationModel
        {
            public int AccountAccountId { get; set; }
            public string AccountIdentification { get; set; }
            public string RoleRoleCode { get; set; }
            public string RoleRoleXML { get; set; }
            public string SendAlertsTo { get; set; }
            public string AvatarSrc { get; set; }
            public bool DefaultOrganization { get; set; }
            public int MemberId { get; set; }
        }

        public class OrganizationInfo
        {
            public string Organization { get; set; }
            public int OrganizationId { get; set; }
            public int MemberId { get; set; }
            public string RoleRoleCode { get; set; }
            public string RoleRoleXML { get; set; }
        }

        public List<OrganizationModel> GetOwnedOrganizations()
        {
            using (var scope = _dbDbContextScopeFactory.Create())
            {
                // TODO this should probably be done another way
                // 1002 = administrator. Onely one of those ever.
                // 1003 = owner
                return
                    scope.Get<Member>()
                        .Where(x => x.User.UserId == Helper.UserId && (x.Role.RoleId == 1002 || x.Role.RoleId == 1003))
                        .ProjectToOrganizationModel()
                        .ToList();
            }
        }

        public List<OrganizationModel> GetOrganizations(int userId)
        {
            using (var scope = _dbDbContextScopeFactory.Create())
            {
                return
                    scope.Get<Member>()
                        .Where(x => x.User.UserId == userId)
                        .ProjectToOrganizationModel()
                        .ToList();
            }
        }
        public OrganizationInfo GetDefaultOrganization(int userId)
        {
            var organizations = GetOrganizations(userId);
            if (organizations.Count == 0) return null;

            // see if one is marked as default
            var theDefault = organizations.FirstOrDefault(x => x.DefaultOrganization);
            if (theDefault == null) theDefault = organizations[0];

            var myDefault = new OrganizationInfo
                {
                    Organization = theDefault.AccountIdentification,
                    OrganizationId = theDefault.AccountAccountId,
                    MemberId = theDefault.MemberId,
                    RoleRoleCode = theDefault.RoleRoleCode,
                    RoleRoleXML = theDefault.RoleRoleXML
                };
            return myDefault;
        }
    }
}
