using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class NoteGroupTemplateMap : IEntityTypeConfiguration<NoteGroupTemplate>
    {
        public void Configure(EntityTypeBuilder<NoteGroupTemplate> builder)
        {
            // Primary Key
            builder.HasKey(t => t.NoteGroupTemplateId);

            // Properties
            builder.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(50);

            // Table & Column Mappings
            builder.ToTable("NoteGroupTemplate");
            builder.Property(t => t.NoteGroupTemplateId).HasColumnName("NoteGroupTemplateId");
            builder.Property(t => t.Description).HasColumnName("Description");
            builder.Property(t => t.Inherits).HasColumnName("Inherits");
            builder.Property(t => t.AccountId).HasColumnName("AccountId");
        }
    }
}
