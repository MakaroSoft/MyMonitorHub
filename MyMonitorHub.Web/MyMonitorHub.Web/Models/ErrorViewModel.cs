namespace MyMonitorHub.Web.Models
{
    public class ErrorViewModel
    {
        public string? ControllerName { get; set; }
        public string? ActionName { get; set; }
        public string? Message { get; set; }
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
