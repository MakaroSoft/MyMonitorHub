using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyMonitorHub.Domain.Service
{
    public class ServiceRequestCache
    {
        private bool _isNew;
        private ServiceRequest _srRow;

        private readonly IDbContextScopeFactory _contextScopeFactory;
        private readonly ILoggerFactory _loggerFactory;

        public ServiceRequestCache(IDbContextScopeFactory contextScopeFactory, ILoggerFactory loggerFactory = null)
        {
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _contextScopeFactory = contextScopeFactory;
        }

        public bool IsNew
        {
            get { return _isNew; }
        }

        public string Notes
        {
            get { return _srRow.Notes; }
            set { _srRow.Notes = value; }
        }

        public ServiceRequest Get(int accountId, int deviceId)
        {
            if (_srRow != null)
            {
                return _srRow;
            }
            _isNew = false;
            _srRow = new ServiceRequestService(_contextScopeFactory, _loggerFactory).Get(accountId, deviceId);

            return _srRow;
        }

        public ServiceRequest GetOrCreate(int accountId, int deviceId)
        {
            if (_srRow != null)
            {
                return _srRow;
            }
            using (var scope = _contextScopeFactory.Create())
            {
                var srs = new ServiceRequestService(_contextScopeFactory, _loggerFactory);
                _srRow = srs.GetOrCreate(accountId, deviceId);
                _isNew = srs.IsNew;

                scope.SaveChanges();
                return _srRow;
            }
        }


        public void Reset()
        {
            _srRow = null;
            _isNew = false;
        }

    } // class
}



