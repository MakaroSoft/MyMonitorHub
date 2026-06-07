using System;
using System.Collections.Generic;
using System.Linq;
using MyMonitorHub.Domain.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace MyMonitorHub.Domain.Tests.Fakes
{
    /// <summary>
    /// In-memory IDbContextScopeFactory used by unit tests so AgentTokenService can run
    /// without a SQL Server. Every scope returned by Create() shares the same backing
    /// store, which is what real EF tracking gives us in production.
    /// </summary>
    public sealed class InMemoryDbContextScopeFactory : IDbContextScopeFactory
    {
        public Dictionary<Type, List<object>> Store { get; } = new();

        public List<T> Set<T>() where T : class
        {
            if (!Store.TryGetValue(typeof(T), out var list))
            {
                list = new List<object>();
                Store[typeof(T)] = list;
            }
            return list.Cast<T>().ToList();
        }

        public IDbContextScope Create(DbContextOption dbContextOption = DbContextOption.JoinExisting)
            => new InMemoryDbContextScope(Store);
    }

    internal sealed class InMemoryDbContextScope : IDbContextScope
    {
        private readonly Dictionary<Type, List<object>> _store;

        public InMemoryDbContextScope(Dictionary<Type, List<object>> store)
        {
            _store = store;
        }

        public IQueryable<TEntity> Get<TEntity>() where TEntity : class
            => ListFor(typeof(TEntity)).OfType<TEntity>().AsQueryable();

        public void Add<TEntity>(TEntity entity) where TEntity : class
            => ListFor(typeof(TEntity)).Add(entity);

        public void Delete<TEntity>(TEntity entity) where TEntity : class
            => ListFor(typeof(TEntity)).Remove(entity);

        public void Delete<TEntity>(object id) where TEntity : class
            => throw new NotImplementedException();

        public TEntity? Find<TEntity>(object id) where TEntity : class
            => throw new NotImplementedException();

        public void Attach<TEntity>(TEntity entity) where TEntity : class { }

        public EntityState GetState<TEntity>(TEntity entity) where TEntity : class
            => EntityState.Unchanged;

        public void RemoveDevice(int deviceId) => throw new NotImplementedException();

        public void SaveChanges() { /* changes already applied to in-memory lists */ }

        public void Dispose() { }

        private List<object> ListFor(Type t)
        {
            if (!_store.TryGetValue(t, out var list))
            {
                list = new List<object>();
                _store[t] = list;
            }
            return list;
        }
    }

    /// <summary>
    /// Minimal IConfiguration that only supports the indexer — which is all AgentTokenService uses.
    /// Avoids pulling Microsoft.Extensions.Configuration into the test project just to set two keys.
    /// </summary>
    internal sealed class TestConfiguration : IConfiguration
    {
        private readonly Dictionary<string, string?> _values;

        public TestConfiguration(Dictionary<string, string?> values)
        {
            _values = values;
        }

        public string? this[string key]
        {
            get => _values.TryGetValue(key, out var v) ? v : null;
            set => _values[key] = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
        public IChangeToken GetReloadToken() => throw new NotImplementedException();
        public IConfigurationSection GetSection(string key) => throw new NotImplementedException();
    }
}
