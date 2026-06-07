using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class CpuUsageMap : IEntityTypeConfiguration<CpuUsage>
    {
        public void Configure(EntityTypeBuilder<CpuUsage> builder)
        {
            // Primary Key
            builder.HasKey(t => t.CpuUsageId);

            // Table & Column Mappings
            builder.ToTable("CpuUsage");
            builder.Property(t => t.CpuUsageId).HasColumnName("CpuUsageId");
            builder.Property(t => t.ItemId).HasColumnName("ItemId");
            builder.Property(t => t.Timestamp).HasColumnName("Timestamp");
            builder.Property(t => t.JsonData).HasColumnName("JsonData");

            // Relationships
            builder.HasOne(t => t.Item)
                .WithMany(t => t.CpuUsages)
                .HasForeignKey(d => d.ItemId);
        }
    }
}
