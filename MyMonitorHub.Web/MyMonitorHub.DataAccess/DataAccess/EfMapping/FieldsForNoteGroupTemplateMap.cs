using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class FieldsForNoteGroupTemplateMap : IEntityTypeConfiguration<FieldsForNoteGroupTemplate>
    {
        public void Configure(EntityTypeBuilder<FieldsForNoteGroupTemplate> builder)
        {
            // Primary Key
            builder.HasKey(t => t.FieldsForNoteGroupId);

            // Table & Column Mappings
            builder.ToTable("FieldsForNoteGroupTemplate");
            builder.Property(t => t.FieldsForNoteGroupId).HasColumnName("FieldsForNoteGroupId");
            builder.Property(t => t.NoteGroupTemplateId).HasColumnName("NoteGroupTemplateId");
            builder.Property(t => t.FieldId).HasColumnName("FieldId");

            // Relationships
            builder.HasOne(t => t.Field)
                .WithMany(t => t.FieldsForNoteGroupTemplates)
                .HasForeignKey(d => d.FieldId);
            builder.HasOne(t => t.NoteGroupTemplate)
                .WithMany(t => t.FieldsForNoteGroupTemplates)
                .HasForeignKey(d => d.NoteGroupTemplateId);
        }
    }
}
