using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MyMonitorHub.Web.Html
{
    /// <summary>
    /// Extension methods to render HTML widgets in Razor views.
    /// Replaces the old System.Web.Mvc.HtmlHelper / MvcHtmlString equivalents.
    /// </summary>
    public static class HtmlExtensions
    {
        public static IHtmlContent StatusDescription(this IHtmlHelper htmlHelper, string statusDescription)
        {
            var diskPattern = new Regex(@"^Free: \d*\.?\d+(GB|MB)\(.+\%\), min: \d*\.?\d+(GB|MB)$");
            if (statusDescription == "")
                return new HtmlString("&nbsp;");

            if (diskPattern.IsMatch(statusDescription))
            {
                var splitPattern = new Regex(@"(\d*\.?\d+|GB|MB|%)");
                var collection = splitPattern.Matches(statusDescription);
                var free = double.Parse(collection[0].Value);
                if (collection[1].Value == "GB") free = Math.Round(free * 1024, MidpointRounding.AwayFromZero);
                var percentFree = double.Parse(collection[2].Value);
                var fail = double.Parse(collection[4].Value);
                if (collection[5].Value == "GB") fail = Math.Round(fail * 1024, MidpointRounding.AwayFromZero);
                var total = Math.Round(free / percentFree * 100, MidpointRounding.AwayFromZero);
                return new HtmlString(FormatDiskHTML(total, free, fail, 0));
            }
            if (statusDescription.StartsWith("_@DISK@_|"))
            {
                var parms = statusDescription.Split(new[] { "|" }, StringSplitOptions.None);
                var total = double.Parse(parms[1]);
                var used = double.Parse(parms[2]);
                var fail = parms.Length > 6 ? double.Parse(parms[6]) : 0d;
                if (parms.Length > 7) fail = double.Parse(parms[7]);
                var free = Math.Round(total - used, MidpointRounding.AwayFromZero);
                return new HtmlString(FormatDiskHTML(total, free, fail, 0));
            }
            if (statusDescription.StartsWith("_@URL@_|"))
            {
                var parms = statusDescription.Split(new[] { "|" }, StringSplitOptions.None);
                var avg = int.Parse(parms[4]);
                return new HtmlString("Response time is " + avg + "ms");
            }
            if (statusDescription.StartsWith("_@URL2@_|"))
            {
                var parms = statusDescription.Split(new[] { "|" }, StringSplitOptions.None);
                return new HtmlString(parms[5]);
            }
            if (statusDescription.StartsWith("_@ERROR@_|"))
            {
                var parms = statusDescription.Split(new[] { "|" }, StringSplitOptions.None);
                var current = int.Parse(parms[1]);
                var recent = int.Parse(parms[2]);
                return new HtmlString("Current error count:  " + current + ", errors within last cycle: " + recent);
            }
            return new HtmlString(statusDescription);
        }

        private static string FormatDiskHTML(double total, double free, double fail, double warn)
        {
            var totalGB = Math.Round(total / 1024, MidpointRounding.AwayFromZero);
            var freeGB = Math.Round(free / 1024, 1, MidpointRounding.AwayFromZero);
            var freePercent = Math.Round(free / total * 100, 1, MidpointRounding.AwayFromZero);
            var freePercentInt = (int)Math.Round(freePercent, MidpointRounding.AwayFromZero);
            var usedPercentInt = 100 - freePercentInt;
            var statusClass = "diskGood";
            if (free < fail) statusClass = "diskFail";
            else if (warn != 0.0 && free < warn) statusClass = "diskWarn";
            var message = "Total = " + totalGB + "GB. Free = " + freeGB + "GB (" + freePercent + "%)";
            return "<table cellpadding=\"0\" cellspacing=\"0\"><tr><td>" +
                "<table width=\"150px\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\" style=\"height: 10px;width: 150px;\">" +
                "<tr>" +
                "<td class=\"" + statusClass + "\" style=\"width: " + usedPercentInt + "%; height: 10px;\"/>" +
                "<td class=\"diskRest\" style=\"height: 10px; width: " + freePercentInt + "%;\" />" +
                "</tr>" +
                "</table></td><td>&nbsp;&nbsp;" + message + "</td></tr></table>";
        }
    }
}
