using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class FieldMap : IEntityTypeConfiguration<Field>
    {
        public void Configure(EntityTypeBuilder<Field> builder)
        {
            // Primary Key
            builder.HasKey(t => t.FieldId);

            // Properties
            builder.Property(t => t.FieldName)
                .IsRequired()
                .HasMaxLength(50);

            // Table & Column Mappings
            builder.ToTable("Field");
            builder.Property(t => t.FieldId).HasColumnName("FieldId");
            builder.Property(t => t.FieldName).HasColumnName("FieldName");
        }
    }
}
