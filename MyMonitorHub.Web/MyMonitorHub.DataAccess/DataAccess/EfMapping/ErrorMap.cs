using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class ErrorMap : IEntityTypeConfiguration<Error>
    {
        public void Configure(EntityTypeBuilder<Error> builder)
        {
            // Primary Key
            builder.HasKey(t => t.ErrorId);

            // Table & Column Mappings
            builder.ToTable("Error");
            builder.Property(t => t.ErrorId).HasColumnName("ErrorId");
            builder.Property(t => t.ItemId).HasColumnName("ItemId");
            builder.Property(t => t.Timestamp).HasColumnName("Timestamp");
            builder.Property(t => t.NewErrorCount).HasColumnName("NewErrorCount");

            // Relationships
            builder.HasOne(t => t.Item)
                .WithMany(t => t.Errors)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
