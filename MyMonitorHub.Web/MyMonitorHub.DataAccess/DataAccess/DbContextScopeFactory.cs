using System.Collections.Generic;
using MyMonitorHub.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace MyMonitorHub.DataAccess
{
    public class DbContextScopeFactory : IDbContextScopeFactory
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        public List<DbContextScope> Contexts = new List<DbContextScope>();

        public DbContextScopeFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<DbContextScopeFactory>();
            MonitorEfContext.Initialize(loggerFactory);
        }

        public IDbContextScope Create(DbContextOption dbContextOption = DbContextOption.JoinExisting)
        {
            IDbContext dbContext;
            var root = false;
            var spaces = new string(' ', Contexts.Count * 4);
            if (Contexts.Count == 0 || dbContextOption == DbContextOption.CreateNew)
            {
                dbContext = string.IsNullOrEmpty(MonitorDbContextConfig.ConnectionString)
                    ? new MonitorEfContext()
                    : new MonitorEfContext(MonitorDbContextConfig.ConnectionString);
                _logger.LogDebug("{0}Created new dbContext = {1}", spaces, dbContext.GetHashCode());
                root = true;
            }
            else
            {
                dbContext = Contexts[Contexts.Count - 1].Uow;
            }
            var scope = new DbContextScope(this, dbContext, root, _loggerFactory);
            Contexts.Add(scope);
            return scope;
        }

    }

}
