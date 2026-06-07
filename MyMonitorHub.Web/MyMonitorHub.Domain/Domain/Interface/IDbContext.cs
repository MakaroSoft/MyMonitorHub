using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace MyMonitorHub.Domain.Interface
{
    public interface IDbContext : IDisposable
    {
        IQueryable<TEntity> Get<TEntity>() where TEntity : class;
        TEntity? Find<TEntity>(object id) where TEntity : class;
        void Add<TEntity>(TEntity entity) where TEntity : class;
        void Delete<TEntity>(TEntity entity) where TEntity : class;
        void Delete<TEntity>(object id) where TEntity : class;
        void Attach<TEntity>(TEntity entity) where TEntity : class;
        EntityState GetState<TEntity>(TEntity entity) where TEntity : class;

        void RemoveDevice(int deviceId);

        int SaveChanges();
    }
}
