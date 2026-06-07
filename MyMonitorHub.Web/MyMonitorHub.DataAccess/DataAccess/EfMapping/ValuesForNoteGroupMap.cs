using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class ValuesForNoteGroupMap : IEntityTypeConfiguration<ValuesForNoteGroup>
    {
        public void Configure(EntityTypeBuilder<ValuesForNoteGroup> builder)
        {
            // Primary Key
            builder.HasKey(t => t.ValuesForNoteGroupId);

            // Properties
            builder.Property(t => t.Value)
                .IsRequired()
                .HasMaxLength(200);

            // Table & Column Mappings
            builder.ToTable("ValuesForNoteGroup");
            builder.Property(t => t.ValuesForNoteGroupId).HasColumnName("ValuesForNoteGroupId");
            builder.Property(t => t.NoteGroupId).HasColumnName("NoteGroupId");
            builder.Property(t => t.Value).HasColumnName("Value");
            builder.Property(t => t.FieldId).HasColumnName("FieldId");

            // Relationships
            builder.HasOne(t => t.Field)
                .WithMany(t => t.ValuesForNoteGroups)
                .HasForeignKey(d => d.FieldId);
            builder.HasOne(t => t.NoteGroup)
                .WithMany(t => t.ValuesForNoteGroups)
                .HasForeignKey(d => d.NoteGroupId);
        }
    }
}
