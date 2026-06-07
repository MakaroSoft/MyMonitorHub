using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class ItemMap : IEntityTypeConfiguration<Item>
    {
        public void Configure(EntityTypeBuilder<Item> builder)
        {
            // Primary Key
            builder.HasKey(t => t.ItemId);

            // Properties
            builder.Property(t => t.SubCategoryName)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(t => t.StatusDescription)
                .IsRequired()
                .HasMaxLength(50);

            // Table & Column Mappings
            builder.ToTable("Item");
            builder.Property(t => t.ItemId).HasColumnName("ItemId");
            builder.Property(t => t.CategoryId).HasColumnName("CategoryId");
            builder.Property(t => t.SubCategoryName).HasColumnName("SubCategoryName");
            builder.Property(t => t.Description).HasColumnName("Description");
            builder.Property(t => t.EventId).HasColumnName("EventId");
            builder.Property(t => t.Status).HasColumnName("Status");
            builder.Property(t => t.StatusDescription).HasColumnName("StatusDescription");
            builder.Property(t => t.TimeStamp).HasColumnName("TimeStamp");
            builder.Property(t => t.DeviceId).HasColumnName("DeviceId");
            builder.Property(t => t.LastServiceRequestId).HasColumnName("LastServiceRequestId");
            builder.Property(t => t.AccountId).HasColumnName("AccountId");
            builder.Property(t => t.ConstantlyReportsInYN).HasColumnName("ConstantlyReportsInYN");

            // Relationships
            builder.HasOne(t => t.Category)
                .WithMany(t => t.Items)
                .HasForeignKey(d => d.CategoryId);
            builder.HasOne(t => t.Device)
                .WithMany(t => t.Items)
                .HasForeignKey(d => d.DeviceId);
            builder.HasOne(t => t.LastServiceRequest)
                .WithMany(t => t.Items)
                .HasForeignKey(d => d.LastServiceRequestId);
        }
    }
}
