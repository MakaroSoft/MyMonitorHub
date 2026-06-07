using System;

namespace MyMonitorHub.Domain.Util
{
    public static class Extensions
    {
        public static string ToShortDateString(this DateTime? myDate)
        {
            if (myDate == null)
            {
                return "";
            }
            return myDate.Value.ToShortDateString();
        }
    }
}