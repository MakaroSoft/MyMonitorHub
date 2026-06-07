using Newtonsoft.Json.Linq;

namespace MyMonitorHub.Agent.Core
{
    public class Dom
    {
        public static string? GetChildElementText(JToken node, string propertyName)
        {
            return node?[propertyName]?.ToString();
        }

        public static string GetChildElementText(JToken node, string propertyName, string deflt)
        {
            var value = node?[propertyName]?.ToString();
            return string.IsNullOrEmpty(value) ? deflt : value;
        }

        public static string? GetAttribute(JToken node, string attributeName, string? dflt = null)
        {
            var value = node?[attributeName]?.ToString();
            return value ?? dflt;
        }
    }
}