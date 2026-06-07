using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class NoteGroupMap : IEntityTypeConfiguration<NoteGroup>
    {
        public void Configure(EntityTypeBuilder<NoteGroup> builder)
        {
            // Primary Key
            builder.HasKey(t => t.NoteGroupId);

            // Properties
            builder.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(50);

            // Table & Column Mappings
            builder.ToTable("NoteGroup");
            builder.Property(t => t.NoteGroupId).HasColumnName("NoteGroupId");
            builder.Property(t => t.DeviceGroupId).HasColumnName("DeviceGroupId");
            builder.Property(t => t.NoteGroupTemplateId).HasColumnName("NoteGroupTemplateId");
            builder.Property(t => t.Description).HasColumnName("Description");

            // Relationships
            builder.HasOne(t => t.DeviceGroup)
                .WithMany(t => t.NoteGroups)
                .HasForeignKey(d => d.DeviceGroupId);
            builder.HasOne(t => t.NoteGroupTemplate)
                .WithMany(t => t.NoteGroups)
                .HasForeignKey(d => d.NoteGroupTemplateId);
        }
    }
}
