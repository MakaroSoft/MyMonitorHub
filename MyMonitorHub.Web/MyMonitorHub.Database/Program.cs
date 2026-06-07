using DbUp;
using Microsoft.Extensions.Configuration;

var configDir = Environment.GetEnvironmentVariable("MYMONITORHUB_WEB_CONFIG_DIR")
    ?? AppContext.BaseDirectory;

var configFilePath = Path.Combine(configDir, "appsettings.json");

if (!File.Exists(configFilePath))
{
    Console.Error.WriteLine($"ERROR: appsettings.json not found at: {configFilePath}");
    Console.Error.WriteLine("Set the MYMONITORHUB_WEB_CONFIG_DIR environment variable to the directory containing appsettings.json.");
    return 1;
}

var config = new ConfigurationBuilder()
    .AddJsonFile(configFilePath, optional: false)
    .Build();

var connectionString = config.GetConnectionString("MonitorContext");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("ERROR: ConnectionStrings:MonitorContext is not configured in appsettings.json.");
    Console.Error.WriteLine($"Config file used: {configFilePath}");
    return 1;
}

EnsureDatabase.For.SqlDatabase(connectionString);

var upgrader = DeployChanges.To
    .SqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(typeof(Program).Assembly)
    .LogToConsole()
    .Build();

var result = upgrader.PerformUpgrade();

if (!result.Successful)
{
    Console.Error.WriteLine($"ERROR: Database upgrade failed: {result.Error.Message}");
    return 1;
}

Console.WriteLine("Database upgrade completed successfully.");
return 0;
