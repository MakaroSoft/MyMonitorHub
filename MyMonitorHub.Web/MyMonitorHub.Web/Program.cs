using MyMonitorHub.DataAccess;
using MyMonitorHub.Domain.BO;
using MyMonitorHub.Domain.Config;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Security;
using MyMonitorHub.Domain.Util;
using MyMonitorHub.Web;
using MyMonitorHub.Web.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Ninject;
using Ninject.Extensions.DependencyInjection;
using Serilog;
using System.Text;

var configDir = Environment.GetEnvironmentVariable("MYMONITORHUB_WEB_CONFIG_DIR");
var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

// Surface Serilog's own errors (e.g. a File sink that can't create/write its
// directory) to stderr, which the host captures. Without this, sink failures
// are swallowed silently and it looks like logging "just doesn't happen".
Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine("[Serilog-SelfLog] " + msg));

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .SetBasePath(configDir ?? AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .AddJsonFile($"appsettings.{aspnetEnv}.json", optional: true)
        .Build())
    .CreateBootstrapLogger();

var logger = Log.Logger;
try
{
    var builder = WebApplication.CreateBuilder(args);

    if (configDir != null)
    {
        builder.Host.ConfigureAppConfiguration((context, config) =>
        {
            config.Sources.Clear();
            config.SetBasePath(configDir)
                  .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                  .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false)
                  .AddEnvironmentVariables()
                  .AddCommandLine(args);
        });
    }

    // Restrict Kestrel to HTTP/1.1 so WebSocket upgrades use the standard
    // GET + "Upgrade: websocket" handshake.  HTTP/2 CONNECT (RFC 8441) is
    // not needed here and causes 405s when the browser negotiates HTTP/2
    // via ALPN before the WebSocket middleware can intercept the request.
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ConfigureEndpointDefaults(listenOptions =>
        {
            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
        });
    });

    // Logging — Serilog
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    // MVC with Razor views
    builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.Add<MyMonitorHub.Domain.Util.CustomExceptionFilter>();
        options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
    })
    .AddNewtonsoftJson();

    // Session
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<AgentAuthFailureTracker>();
    builder.Services.AddSingleton<LoginFailureTracker>();
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        AgentAuthRateLimitPolicies.AddAgentAuthPolicies(options);
        AgentAuthRateLimitPolicies.AddLoginPolicies(options);
        AgentAuthRateLimitPolicies.AddAccountPolicies(options);
    });
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromHours(8);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    });

    // Authentication — cookie for browser users, JWT Bearer for agents
    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret is not configured in appsettings.");
    if (jwtSecret.Length < 32)
        throw new InvalidOperationException("Jwt:Secret must be at least 32 characters. Set it via environment variable or user secrets, never in appsettings.json.");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "mymonitorhub";
    var jwtAudience = builder.Configuration["Jwt:Audience"]
        ?? throw new InvalidOperationException("Jwt:Audience is not configured in appsettings.");

    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Account/LogOn";
            options.LogoutPath = "/Account/LogOff";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Cookie.Name = "__Host-monitor";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            // The "__Host-" name prefix requires the cookie to be Secure, have no Domain,
            // and use Path "/". Behind a sub-application (e.g. /mymonitorhub) the cookie
            // path otherwise defaults to the PathBase, which violates the prefix rule and
            // makes the browser silently drop the auth cookie — leaving the user
            // unauthenticated and bounced back to LogOn after a successful 2FA.
            options.Cookie.Path = "/";
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
                RequireSignedTokens = true,
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
            };
        });

    builder.Services.AddAuthorization();

    // Data protection (replaces MachineKey).
    // The session cookie, antiforgery tokens and the auth cookie are all encrypted
    // with this key ring. Keys MUST be persisted to a stable location so they survive
    // app-pool recycles/deploys and are shared across worker processes/instances —
    // otherwise the session written during POST /Account/LogOn cannot be decrypted on
    // the GET /Account/Get2Fa redirect and login silently bounces back to LogOn.
    var dataProtection = builder.Services.AddDataProtection()
        .SetApplicationName("MyMonitorHub");

    var dpKeysDir = builder.Configuration["DataProtection:KeysDirectory"]
        ?? (configDir != null ? Path.Combine(configDir, "keys") : null);
    if (!string.IsNullOrWhiteSpace(dpKeysDir))
    {
        Directory.CreateDirectory(dpKeysDir);
        dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dpKeysDir));
        Log.Information("Data Protection keys are persisted to {KeysDirectory}", dpKeysDir);
    }
    else
    {
        Log.Warning("Data Protection keys are NOT being persisted (neither DataProtection:KeysDirectory nor MYMONITORHUB_WEB_CONFIG_DIR is set). " +
                    "Keys will be ephemeral, so sessions, antiforgery tokens and auth cookies will break across restarts, recycles and instances.");
    }

    // HttpContextAccessor for static Helper access
    builder.Services.AddHttpContextAccessor();

    // Ninject as the DI container (Ninject.Extensions.DependencyInjection 1.0.2 API)
    builder.Host.UseServiceProviderFactory(new NinjectServiceProviderFactory());
    builder.Host.ConfigureContainer<IKernel>(kernel =>
    {
        kernel.Settings.AllowNullInjection = true;
        kernel.Bind<IDbContextScopeFactory>().To<DbContextScopeFactory>();
        kernel.Bind<WebSocketTicketFactory>().ToSelf().InSingletonScope();
        kernel.Bind<IMonitorAuthService>().To<MonitorAuthService>();
    });

    var app = builder.Build();

    // EF6 code-based connection string (replaces web.config <connectionStrings>)
    var efConnectionString = app.Configuration.GetConnectionString("MonitorContext")
        ?? throw new InvalidOperationException("Missing connection string 'MonitorContext' in configuration.");
    MonitorDbContextConfig.Configure(efConnectionString);

    // Outgoing email settings (replaces the SystemConfig database table)
    var emailSettings = app.Configuration.GetSection("OutgoingEmail").Get<OutgoingEmailSettings>()
        ?? new OutgoingEmailSettings();
    OutgoingEmailConfig.Configure(emailSettings);

    // Resolve ILoggerFactory for singleton initializers
    var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

    // Initialize singleton / static classes with the DI-resolved logger factory
    AgentConnections.Initialize(loggerFactory);
    // Initialize TardyThread background service with the DI-resolved factory
    var tardyFactory = app.Services.GetRequiredService<IDbContextScopeFactory>();
    TardyThread.Initialize(tardyFactory, loggerFactory, app.Configuration);

    // Configure static Helper with IHttpContextAccessor and web root path
    var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();
    Helper.Configure(httpContextAccessor, app.Environment.WebRootPath, loggerFactory);

    // Middleware pipeline
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error/Custom");
        app.UseHsts();
    }
    app.UseWebSockets();
    app.UseHttpsRedirection();

    // Security headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https://www.gravatar.com; " +
            "font-src 'self'; " +
            "connect-src 'self' wss:; " +
            "frame-ancestors 'none';";
        await next();
    });

    app.UseStaticFiles();
    app.UseRouting();
    app.UseRateLimiter();
    app.UseSession();
    app.UseAuthentication();
    app.UseAuthorization();

    // Populate ThreadStaticHelper.RootPath/RootUrl per request (replaces Application_BeginRequest)
    app.Use(async (context, next) =>
    {
        ThreadStaticHelper.RootPath = app.Environment.WebRootPath;
        var request = context.Request;
        var rootUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";
        if (!rootUrl.EndsWith('/')) rootUrl += "/";
        ThreadStaticHelper.RootUrl = rootUrl;
        await next();
    });

    // Route: api/{controller}/{action}/{id?} (replaces WebApiConfig)
    app.MapControllerRoute(
        name: "api",
        pattern: "api/{controller}/{action}/{id?}");

    // Route: {controller}/{action}/{id?} (replaces RouteConfig default)
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application startup failed.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
