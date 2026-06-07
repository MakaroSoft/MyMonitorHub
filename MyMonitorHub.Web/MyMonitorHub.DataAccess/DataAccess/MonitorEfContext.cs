using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyMonitorHub.DataAccess.EfMapping;
using MyMonitorHub.Domain.Entity;
using MyMonitorHub.Domain.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyMonitorHub.DataAccess
{
    public class MonitorEfContext : DbContext, IDbContext
    {
        private static ILogger _logger = NullLogger.Instance;

        public static void Initialize(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MonitorEfContext>();
        }

        public MonitorEfContext()
        {
        }

        public MonitorEfContext(string connectionString)
            : base(new DbContextOptionsBuilder<MonitorEfContext>()
                .UseLazyLoadingProxies()
                .UseSqlServer(connectionString)
                .Options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder
                    .UseLazyLoadingProxies()
                    .UseSqlServer(MonitorDbContextConfig.ConnectionString);
            }
        }

        public void RemoveDevice(int deviceId)
        {
            Database.ExecuteSqlRaw("RemoveDevice @deviceId", new SqlParameter("deviceId", deviceId));
        }

        public IQueryable<TEntity> Get<TEntity>() where TEntity : class
        {
            return Set<TEntity>();
        }

        public TEntity? Find<TEntity>(object id) where TEntity : class
        {
            return Set<TEntity>().Find(id);
        }

        public void Delete<TEntity>(TEntity entity) where TEntity : class
        {
            Set<TEntity>().Remove(entity);
        }

        public void Delete<TEntity>(object id) where TEntity : class
        {
            var entity = Find<TEntity>(id);
            if (entity != null)
                Delete(entity);
        }

        public void Add<TEntity>(TEntity entity) where TEntity : class
        {
            Set<TEntity>().Add(entity);
        }

        public void Attach<TEntity>(TEntity entity) where TEntity : class
        {
            if (Entry(entity).State == EntityState.Detached)
            {
                Set<TEntity>().Attach(entity);
            }
        }

        public EntityState GetState<TEntity>(TEntity entity) where TEntity : class
        {
            return Entry(entity).State;
        }

        public new int SaveChanges()
        {
            var entries = ChangeTracker.Entries();
            foreach (var entry in entries)
            {
                if (entry.State != EntityState.Unchanged)
                {
                    var name = entry.Entity.GetType().Name;
                    var index = name.IndexOf("_", StringComparison.Ordinal);
                    if (index != -1)
                    {
                        name = name.Substring(0, index);
                    }
                    _logger.LogDebug("{0} - {1}", name, entry.State);
                }
            }

            return base.SaveChanges();
        }

        public DbSet<Contact> Contacts { get; set; }
        public DbSet<PhoneType> PhoneTypes { get; set; }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Configuration> Configurations { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceRefreshToken> DeviceRefreshTokens { get; set; }
        public DbSet<DeviceGroup> DeviceGroups { get; set; }
        public DbSet<DeviceType> DeviceTypes { get; set; }
        public DbSet<DiskUsage> DiskUsages { get; set; }
        public DbSet<CpuUsage> CpuUsages { get; set; }
        public DbSet<Error> Errors { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Field> Fields { get; set; }
        public DbSet<FieldsForNoteGroupTemplate> FieldsForNoteGroupTemplates { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<MonthlyReport> MonthlyReports { get; set; }
        public DbSet<NoteGroup> NoteGroups { get; set; }
        public DbSet<NoteGroupTemplate> NoteGroupTemplates { get; set; }
        public DbSet<Page> Pages { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }
        public DbSet<UrlSpeed> UrlSpeeds { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ValuesForNoteGroup> ValuesForNoteGroups { get; set; }
        public DbSet<EmailNotification> EmailNotifications { get; set; }
        public DbSet<PendingRegistration> PendingRegistrations { get; set; }
        public DbSet<PendingPasswordReset> PendingPasswordResets { get; set; }
        public DbSet<UserRecoveryCode> UserRecoveryCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new AccountMap());
            modelBuilder.ApplyConfiguration(new CategoryMap());
            modelBuilder.ApplyConfiguration(new ConfigurationMap());
            modelBuilder.ApplyConfiguration(new CpuUsageMap());
            modelBuilder.ApplyConfiguration(new DeviceMap());
            modelBuilder.ApplyConfiguration(new DeviceRefreshTokenMap());
            modelBuilder.ApplyConfiguration(new DeviceGroupMap());
            modelBuilder.ApplyConfiguration(new DeviceTypeMap());
            modelBuilder.ApplyConfiguration(new DiskUsageMap());
            modelBuilder.ApplyConfiguration(new ErrorMap());
            modelBuilder.ApplyConfiguration(new EventMap());
            modelBuilder.ApplyConfiguration(new FieldMap());
            modelBuilder.ApplyConfiguration(new FieldsForNoteGroupTemplateMap());
            modelBuilder.ApplyConfiguration(new ItemMap());
            modelBuilder.ApplyConfiguration(new MemberMap());
            modelBuilder.ApplyConfiguration(new MonthlyReportMap());
            modelBuilder.ApplyConfiguration(new NoteGroupMap());
            modelBuilder.ApplyConfiguration(new NoteGroupTemplateMap());
            modelBuilder.ApplyConfiguration(new PageMap());
            modelBuilder.ApplyConfiguration(new RoleMap());
            modelBuilder.ApplyConfiguration(new ServiceRequestMap());
            modelBuilder.ApplyConfiguration(new UrlSpeedMap());
            modelBuilder.ApplyConfiguration(new UserMap());
            modelBuilder.ApplyConfiguration(new ValuesForNoteGroupMap());
            modelBuilder.ApplyConfiguration(new EmailNotificationMap());
            modelBuilder.ApplyConfiguration(new ContactMap());
            modelBuilder.ApplyConfiguration(new PhoneTypeMap());
            modelBuilder.ApplyConfiguration(new PendingRegistrationMap());
            modelBuilder.ApplyConfiguration(new PendingPasswordResetMap());
            modelBuilder.ApplyConfiguration(new UserRecoveryCodeMap());
        }
    }
}
