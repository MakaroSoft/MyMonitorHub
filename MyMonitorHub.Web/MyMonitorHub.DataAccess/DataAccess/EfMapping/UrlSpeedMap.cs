using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class UrlSpeedMap : IEntityTypeConfiguration<UrlSpeed>
    {
        public void Configure(EntityTypeBuilder<UrlSpeed> builder)
        {
            // Primary Key
            builder.HasKey(t => t.UrlSpeedId);

            // Table & Column Mappings
            builder.ToTable("UrlSpeed");
            builder.Property(t => t.UrlSpeedId).HasColumnName("UrlSpeedId");
            builder.Property(t => t.ItemId).HasColumnName("ItemId");
            builder.Property(t => t.Timestamp).HasColumnName("Timestamp");
            builder.Property(t => t.CurSpeedMS).HasColumnName("CurSpeedMS");
            builder.Property(t => t.MinSpeedMS).HasColumnName("MinSpeedMS");
            builder.Property(t => t.MaxSpeedMS).HasColumnName("MaxSpeedMS");
            builder.Property(t => t.AvgSpeedMS).HasColumnName("AvgSpeedMS");

            // Relationships
            builder.HasOne(t => t.Item)
                .WithMany(t => t.UrlSpeeds)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
