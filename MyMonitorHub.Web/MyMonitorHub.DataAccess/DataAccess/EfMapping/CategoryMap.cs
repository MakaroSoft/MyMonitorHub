using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class CategoryMap : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            // Primary Key
            builder.HasKey(t => t.CategoryId);

            // Properties
            builder.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(50);

            // Table & Column Mappings
            builder.ToTable("Category");
            builder.Property(t => t.CategoryId).HasColumnName("CategoryId");
            builder.Property(t => t.AccountId).HasColumnName("AccountId");
            builder.Property(t => t.Description).HasColumnName("Description");
        }
    }
}
