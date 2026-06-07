using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyMonitorHub.Domain.Interface;
using Microsoft.Extensions.Logging;

namespace MyMonitorHub.DataAccess
{
    public class DbContextScope : IDbContextScope
    {
        private readonly ILogger _logger;

        private bool _changesSaved;
        private bool _disposed;
        public readonly IDbContext Uow;
        private readonly bool _root;
        private readonly DbContextScopeFactory _dbContextScopeFactory;
        private readonly string _spaces;

        public DbContextScope(DbContextScopeFactory dbContextScopeFactory, IDbContext uow, bool root, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DbContextScope>();
            _spaces = new string(' ', dbContextScopeFactory.Contexts.Count * 4);

            _logger.LogDebug("{0}Constructor of contextScope: {1}", _spaces, GetHashCode());
            _dbContextScopeFactory = dbContextScopeFactory;
            Uow = uow;
            _logger.LogDebug("{0}using dbContext: {1}", _spaces, Uow.GetHashCode());
            _root = root;
        }

        public void Dispose()
        {
            _logger.LogDebug("{0}contextScope dispose: {1}", _spaces, GetHashCode());
            CheckDisposed();
            var count = _dbContextScopeFactory.Contexts.Count;
            if (count == 0)
            {
                throw new Exception("WTF happened to my scope?");
            }
            if (count == 0 || _dbContextScopeFactory.Contexts[count - 1] != this)
            {
                throw new Exception("Cannot dispose scope out of order.");
            }
            _dbContextScopeFactory.Contexts.Remove(this);
            if (_root)
            {
                _logger.LogDebug("{0}disposing dbContext: {1}", _spaces, Uow.GetHashCode());
                Uow.Dispose();
            }
            _disposed = true;
            _logger.LogDebug("");
        }

        public void SaveChanges()
        {
            CheckDisposed();
            if (_changesSaved) throw new Exception("Only one SaveChanges() allowed per scope");
            if (_root)
            {
                _logger.LogDebug("{0}Save Changes root", _spaces);
                Uow.SaveChanges();
                _changesSaved = true;
            }
            else
            {
                _logger.LogDebug("{0}Save Changes but not root", _spaces);
            }
        }

        public IQueryable<TEntity> Get<TEntity>() where TEntity : class
        {
            CheckDisposed();
            return Uow.Get<TEntity>();
        }
        public TEntity? Find<TEntity>(object id) where TEntity : class
        {
            CheckDisposed();
            return Uow.Find<TEntity>(id);
        }

        public void Add<TEntity>(TEntity entity) where TEntity : class
        {
            CheckDisposed();
            Uow.Add(entity);
        }
        public void Delete<TEntity>(TEntity entity) where TEntity : class
        {
            CheckDisposed();
            Uow.Delete(entity);
        }
        public void Delete<TEntity>(object id) where TEntity : class
        {
            CheckDisposed();
            var entity = Find<TEntity>(id);
            Uow.Delete(entity);
        }
        public void Attach<TEntity>(TEntity entity) where TEntity : class
        {
            CheckDisposed();
            Uow.Attach(entity);
        }

        public EntityState GetState<TEntity>(TEntity entity) where TEntity : class
        {
            CheckDisposed();
            return Uow.GetState(entity);
        }

        public void RemoveDevice(int deviceId)
        {
            CheckDisposed();
            Uow.RemoveDevice(deviceId);
        }

        private void CheckDisposed()
        {
            if (_disposed) throw new Exception("Scope has been disposed");            
        }
    }
}
