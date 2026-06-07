using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class MonthlyReportMap : IEntityTypeConfiguration<MonthlyReport>
    {
        public void Configure(EntityTypeBuilder<MonthlyReport> builder)
        {
            // Primary Key
            builder.HasKey(t => t.MonthlyReportId);

            // Table & Column Mappings
            builder.ToTable("MonthlyReport");
            builder.Property(t => t.MonthlyReportId).HasColumnName("MonthlyReportId");
            builder.Property(t => t.AccountId).HasColumnName("AccountId");
            builder.Property(t => t.DeviceGroupId).HasColumnName("DeviceGroupId");
            builder.Property(t => t.Year).HasColumnName("Year");
            builder.Property(t => t.Month).HasColumnName("Month");
        }
    }
}
