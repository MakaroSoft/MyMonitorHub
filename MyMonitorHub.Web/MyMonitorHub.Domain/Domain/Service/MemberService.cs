using System.Collections.Generic;
using System.Linq;
using MyMonitorHub.Domain.Util;
using MyMonitorHub.Domain.Interface;
using Member = MyMonitorHub.Domain.Entity.Member;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.Domain.Service
{
    public sealed class MemberService
    {
        private readonly IDbContextScopeFactory _dbContextScopeFactory;
        public MemberService(IDbContextScopeFactory dbContextScopeFactory)
        {
            _dbContextScopeFactory = dbContextScopeFactory;
        }

        public class MemberModel
        {
            public int MemberId { get; set; }
            public string UserEmail { get; set; }
            public int RoleId { get; set; }
            public string RoleRoleCode { get; set; }
            public string SendAlertsTo { get; set; }
            public string Avatar { get; set; }
            public int UserUserId { get; set; }
            public bool? UserTwoFactorEnabled { get; set; }

            public List<EmailNotification> UserEmailNotifications { get; set; }
        }

        public bool RemoveMember(int memberId, int organizationId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var member = scope.Get<Member>()
                    .FirstOrDefault(x => x.MemberId == memberId && x.Account.AccountId == organizationId);
                if (member == null)
                    return false;

                var userId = member.UserId;
                var hasOtherMemberships = scope.Get<Member>()
                    .Any(x => x.UserId == userId && x.MemberId != memberId);

                scope.Delete(member);

                if (!hasOtherMemberships)
                {
                    foreach (var sr in scope.Get<ServiceRequest>().Where(x => x.AssignedToId == userId).ToList())
                        sr.AssignedToId = null;

                    foreach (var en in scope.Get<EmailNotification>().Where(x => x.UserId == userId).ToList())
                        scope.Delete(en);

                    scope.Delete<User>(userId);
                }

                scope.SaveChanges();
                return true;
            }
        }

        public void UpdateMemberRole(int memberId, int roleId, int organizationId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                var member = scope.Get<Member>()
                    .FirstOrDefault(x => x.MemberId == memberId && x.Account.AccountId == organizationId);
                if (member != null)
                {
                    member.RoleId = roleId;
                    scope.SaveChanges();
                }
            }
        }

        public List<MemberModel> GetAllForOrganization(int organizationId, int? roleId)
        {
            using (var scope = _dbContextScopeFactory.Create())
            {
                if (roleId != null)
                {
                    return scope.Get<Member>().Where(x => x.Account.AccountId == organizationId && x.RoleId == roleId).ProjectToMemberModel().ToList();
                }
                return scope.Get<Member>().Where(x => x.Account.AccountId == organizationId).ProjectToMemberModel().ToList();
            }
        }
    }
}
