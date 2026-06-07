namespace MyMonitorHub.Domain.Security
{
    public class PageAccess
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public AccessCode[] AccessCodes { get; set; }
    }

    public class AccessCode
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }
}