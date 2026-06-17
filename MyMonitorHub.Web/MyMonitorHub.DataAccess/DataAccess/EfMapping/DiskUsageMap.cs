using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class DiskUsageMap : IEntityTypeConfiguration<DiskUsage>
    {
        public void Configure(EntityTypeBuilder<DiskUsage> builder)
        {
            // Primary Key
            builder.HasKey(t => t.DiskUsageId);

            // Table & Column Mappings
            builder.ToTable("DiskUsage");
            builder.Property(t => t.DiskUsageId).HasColumnName("DiskUsageId");
            builder.Property(t => t.ItemId).HasColumnName("ItemId");
            builder.Property(t => t.Timestamp).HasColumnName("Timestamp");
            builder.Property(t => t.DiskSize).HasColumnName("DiskSize");
            builder.Property(t => t.CurUsedMB).HasColumnName("CurUsedMB");
            builder.Property(t => t.MinUsedMB).HasColumnName("MinUsedMB");
            builder.Property(t => t.MaxUsedMB).HasColumnName("MaxUsedMB");
            builder.Property(t => t.AvgUsedMB).HasColumnName("AvgUsedMB");

            // Relationships
            builder.HasOne(t => t.Item)
                .WithMany(t => t.DiskUsages)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
