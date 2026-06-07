using System;
using System.Collections.Generic;
using MyMonitorHub.Domain.Dto;
using MyMonitorHub.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace MyMonitorHub.Domain.Service
{
    public class UtilService
    {
        private readonly IDbContextScopeFactory _dbContextScopeFactory;
        private readonly ILogger _logger;

        public UtilService(IDbContextScopeFactory dbContextScopeFactory, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UtilService>();
            _dbContextScopeFactory = dbContextScopeFactory;
        }

        public class UserSummary
        {
            public int UserId { get; set; }
            public string Email { get; set; }
            public int RoleRoleId { get; set; }
        }
    }
}
