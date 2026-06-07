using System;
using System.Linq;
using System.Threading;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using MyMonitorHub.Domain.Service;
using MyMonitorHub.Domain.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Emailer = MyMonitorHub.Domain.Service.Emailer;
using EventService = MyMonitorHub.Domain.Service.EventService;

// DependencyResolver (System.Web.Mvc) is not available in .NET 10 — replaced by static factory holder

namespace MyMonitorHub.Domain.BO
{
    public class TardyThread
    {
        private static ILogger _logger = NullLogger.Instance;
        private static ILoggerFactory? _loggerFactory;

        public static TardyThread Current = new TardyThread();
        private readonly object _lockObject = new object();

        private static IDbContextScopeFactory? _contextScopeFactory;
        private static IConfiguration? _configuration;

        public static void Initialize(IDbContextScopeFactory contextScopeFactory, ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _contextScopeFactory = contextScopeFactory;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<TardyThread>();
            _configuration = configuration;
        }

        private Thread _myThread;

        private string _rootPath;
        private string _rootUrl;
        private bool _shutdown;

        private TardyThread()
        {
        }

        public void Start()
        {
            lock (_lockObject)
            {
                if (_myThread == null)
                {
                    // save these fields
                    _rootUrl = ThreadStaticHelper.RootUrl;
                    _rootPath = ThreadStaticHelper.RootPath;

                    _shutdown = false;
                    _myThread = new Thread(Run);
                    _myThread.Start();
                }
            }
        }

        private void Run()
        {
            ThreadStaticHelper.RootUrl = _rootUrl;
            ThreadStaticHelper.RootPath = _rootPath;

            _logger.LogInformation("findTardy thread started");
            try
            {
                while (!_shutdown)
                {
                    FindTardy(); // this could take a while so I don't want it locked
                    lock (_lockObject)
                    {
                        System.Threading.Monitor.Wait(_lockObject, 1000*60*2); // run every two minutes
                    }
                }
            }
            finally
            {
                lock (_lockObject)
                {
                    _myThread = null;
                    _logger.LogInformation("findTardy thread ended");
                }
            }
        }

        private static void FindTardy() // every two minutes
        {
            if (_contextScopeFactory == null)
            {
                _logger.LogWarning("TardyThread.FindTardy: contextScopeFactory not initialized, skipping.");
                return;
            }
            try
            {
                new EventService(_contextScopeFactory, _loggerFactory!).FindTardy();
                new ServiceRequestService(_contextScopeFactory, _loggerFactory!).FindTardy(); // find ones that are not accepted
            }
            catch (Exception ex1)
            {
                _logger.LogError(ex1, "findTardy failed");
                if (ex1.InnerException != null)
                    _logger.LogError(ex1.InnerException, "findTardy inner exception");

                try
                {
                    var ownerEmail = GetOwnerEmail();
                    if (ownerEmail != null)
                    {
                        var emailer = new Emailer(_contextScopeFactory!, _loggerFactory!);
                        emailer.SendDirect(ownerEmail, "Failure in findTardy", ex1.Message);
                    }
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "findTardy: failed to send alert email");
                }
            }
        }

        private static string? GetOwnerEmail()
        {
            if (_contextScopeFactory == null || _configuration == null)
                return null;

            var ownerOrgId = _configuration.GetValue<int>("App:OwnerOrganizationId");
            if (ownerOrgId == 0)
                return null;

            using var scope = _contextScopeFactory.Create();
            return scope.Get<Member>()
                .Where(m => m.OrganizationId == ownerOrgId && m.Role.RoleCode == "Owner")
                .Select(m => m.User.Email)
                .FirstOrDefault();
        }

    } // class
}