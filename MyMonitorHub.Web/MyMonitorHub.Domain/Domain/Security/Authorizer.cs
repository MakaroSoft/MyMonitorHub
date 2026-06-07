using System;
using MyMonitorHub.Domain.Util;

namespace MyMonitorHub.Domain.Security
{
    public class Authorizer
    {
        public static bool IsAdministrator
        {
            get { return Helper.IsAdministrator; }
        }

        public static bool IsOwner
        {
            get
            {
                if (Helper.IsAdministrator) return true;
                return Helper.IsOwner;
            }
        }

        public static bool Authorize(Permission permission)
        {
            try
            {
                if (IsAdministrator) return true;
                if (IsOwner) return true;

                var pageCode = permission.Group.Name;
                var code = permission.Name;

                var doc = Helper.RoleDocument;
                if (doc == null) return false;

                var node = doc.SelectSingleNode("/pages/page[@code='" + pageCode + "']/access[@code='" + code + "']");
                if (node == null)
                {
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}