using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class PageMap : IEntityTypeConfiguration<Page>
    {
        public void Configure(EntityTypeBuilder<Page> builder)
        {
            // Primary Key
            builder.HasKey(t => t.PageId);

            // Properties
            builder.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(50);

            // Table & Column Mappings
            builder.ToTable("Page");
            builder.Property(t => t.PageId).HasColumnName("PageId");
            builder.Property(t => t.ParentId).HasColumnName("ParentId");
            builder.Property(t => t.Description).HasColumnName("Description");
            builder.Property(t => t.ChildIndex).HasColumnName("ChildIndex");
            builder.Property(t => t.AccountId).HasColumnName("AccountId");
        }
    }
}
