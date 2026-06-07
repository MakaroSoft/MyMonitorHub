using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class DeviceMap : IEntityTypeConfiguration<Device>
    {
        public void Configure(EntityTypeBuilder<Device> builder)
        {
            // Primary Key
            builder.HasKey(t => t.DeviceId);

            // Properties
            builder.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(50);

            // Table & Column Mappings
            builder.ToTable("Device");
            builder.Property(t => t.DeviceId).HasColumnName("DeviceId");
            builder.Property(t => t.ApiKey).HasColumnName("ApiKey").HasMaxLength(36);
            builder.Property(t => t.DeviceGroupId).HasColumnName("DeviceGroupId");
            builder.Property(t => t.Description).HasColumnName("Description");
            builder.Property(t => t.ChildIndex).HasColumnName("ChildIndex");
            builder.Property(t => t.AccountId).HasColumnName("AccountId");
            builder.Property(t => t.DeviceTypeId).HasColumnName("DeviceTypeId");
            builder.Property(t => t.Deleted).HasColumnName("Deleted");
            builder.Property(t => t.WebUrl).HasColumnName("WebUrl");
            builder.Property(t => t.Notes).HasColumnName("Notes");
            builder.Property(t => t.ContactInformation).HasColumnName("ContactInformation");
            builder.Property(t => t.PauseUntilDateTime).HasColumnName("PauseUntilDateTime");
            builder.Property(t => t.ConfigXml).HasColumnName("ConfigXml");

            // Relationships
            builder.HasOne(t => t.DeviceGroup)
                .WithMany(t => t.Devices)
                .HasForeignKey(d => d.DeviceGroupId)
                .IsRequired(false);
            builder.HasOne(t => t.DeviceType)
                .WithMany(t => t.Devices)
                .HasForeignKey(d => d.DeviceTypeId)
                .IsRequired(false);
        }
    }
}
