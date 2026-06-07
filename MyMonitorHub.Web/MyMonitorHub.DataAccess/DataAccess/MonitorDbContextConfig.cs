namespace MyMonitorHub.DataAccess
{
    /// <summary>
    /// Static holder for the EF6 connection string, populated at startup from IConfiguration
    /// (replaces the web.config &lt;connectionStrings&gt; source for MonitorEfContext).
    /// </summary>
    public static class MonitorDbContextConfig
    {
        public static string ConnectionString { get; private set; } = string.Empty;

        public static void Configure(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }
}
