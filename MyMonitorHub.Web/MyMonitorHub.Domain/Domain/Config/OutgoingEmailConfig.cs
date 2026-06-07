namespace MyMonitorHub.Domain.Config
{
    /// <summary>
    /// Holds the outgoing SMTP settings loaded from appsettings.json at startup.
    /// Initialized once via <see cref="Configure"/> before any email service is used.
    /// </summary>
    public static class OutgoingEmailConfig
    {
        public static string Host { get; private set; } = string.Empty;
        public static string From { get; private set; } = string.Empty;
        public static string Username { get; private set; } = string.Empty;
        public static string Password { get; private set; } = string.Empty;

        public static void Configure(OutgoingEmailSettings settings)
        {
            Host = settings.Host;
            From = settings.From;
            Username = settings.Username;
            Password = settings.Password;
        }
    }
}
