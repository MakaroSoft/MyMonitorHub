using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace MyMonitorHub.Domain.Service
{
    public class PhoneTypeService
    {
        // ReSharper disable once UnusedMember.Local
        private readonly ILogger _logger;

        private readonly IDbContextScopeFactory _dbContextScopeFactory;
        public PhoneTypeService(IDbContextScopeFactory dbContextScopeFactory, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<PhoneTypeService>();
            _dbContextScopeFactory = dbContextScopeFactory;
        }

        public List<PhoneType> Get()
        {
            using (var scope = _dbContextScopeFactory.Create(DbContextOption.CreateNew))
            {
                return scope.Get<PhoneType>().ToList();
            }
        }
    }
}
