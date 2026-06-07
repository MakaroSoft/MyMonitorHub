namespace MyMonitorHub.Domain.Config
{
    /// <summary>
    /// Bound from the "OutgoingEmail" section in appsettings.json.
    /// </summary>
    public class OutgoingEmailSettings
    {
        public string Host { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
