using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class DeviceGroupMap : IEntityTypeConfiguration<DeviceGroup>
    {
        public void Configure(EntityTypeBuilder<DeviceGroup> builder)
        {
            // Primary Key
            builder.HasKey(t => t.DeviceGroupId);

            // Properties
            builder.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(t => t.emailsForClosedSRs)
                .HasMaxLength(250);

            // Table & Column Mappings
            builder.ToTable("DeviceGroup");
            builder.Property(t => t.DeviceGroupId).HasColumnName("DeviceGroupId");
            builder.Property(t => t.PageId).HasColumnName("PageId");
            builder.Property(t => t.ChildIndex).HasColumnName("ChildIndex");
            builder.Property(t => t.AccountId).HasColumnName("AccountId");
            builder.Property(t => t.Description).HasColumnName("Description");
            builder.Property(t => t.emailsForClosedSRs).HasColumnName("emailsForClosedSRs");
            builder.Property(t => t.Notes).HasColumnName("Notes");
            builder.Property(t => t.ContactInformation).HasColumnName("ContactInformation");

            // Relationships
            builder.HasOne(t => t.Page)
                .WithMany(t => t.DeviceGroups)
                .HasForeignKey(d => d.PageId);
        }
    }
}
