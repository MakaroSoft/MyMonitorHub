using System.Collections.Generic;
using MyMonitorHub.Domain.Service;

namespace MyMonitorHub.Web.Models
{
    public class RoleMaintModel
    {
        public List<Domain.Security.PageAccess> Layout;
        public RoleService.RoleMaintData Data;
    }
}