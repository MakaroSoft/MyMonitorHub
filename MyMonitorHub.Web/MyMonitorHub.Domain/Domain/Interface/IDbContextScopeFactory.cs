namespace MyMonitorHub.Domain.Interface
{
    public interface IDbContextScopeFactory
    {
        IDbContextScope Create(DbContextOption dbContextOption = DbContextOption.JoinExisting);
    }
}
