using System.Collections.Generic;
using MyMonitorHub.Domain.Dto;

namespace MyMonitorHub.Web.Models
{
    public class RolesModel
    {
        public List<RoleListModel> SystemRoles { get; set; }
        public List<RoleListModel> CustomRoles { get; set; }
    }
}