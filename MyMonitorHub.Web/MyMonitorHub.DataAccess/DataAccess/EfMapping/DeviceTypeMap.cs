using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class DeviceTypeMap : IEntityTypeConfiguration<DeviceType>
    {
        public void Configure(EntityTypeBuilder<DeviceType> builder)
        {
            // Primary Key
            builder.HasKey(t => t.DeviceTypeId);

            // Properties
            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(50);

            // Table & Column Mappings
            builder.ToTable("DeviceType");
            builder.Property(t => t.DeviceTypeId).HasColumnName("DeviceTypeId");
            builder.Property(t => t.Name).HasColumnName("Name");
        }
    }
}
