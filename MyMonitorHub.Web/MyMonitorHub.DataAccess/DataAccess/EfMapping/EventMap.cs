using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class EventMap : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            // Primary Key
            builder.HasKey(t => t.EventId);

            // Properties
            builder.Property(t => t.Category)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(t => t.SubCategory)
                .HasMaxLength(20);

            builder.Property(t => t.ItemName)
                .HasMaxLength(50);

            builder.Property(t => t.StatusDescription)
                .IsRequired();

            // Table & Column Mappings
            builder.ToTable("Event");
            builder.Property(t => t.EventId).HasColumnName("EventId");
            builder.Property(t => t.AccountId).HasColumnName("AccountId");
            builder.Property(t => t.DeviceId).HasColumnName("DeviceId");
            builder.Property(t => t.Category).HasColumnName("Category");
            builder.Property(t => t.SubCategory).HasColumnName("SubCategory");
            builder.Property(t => t.ItemName).HasColumnName("ItemName");
            builder.Property(t => t.Status).HasColumnName("Status");
            builder.Property(t => t.StatusDescription).HasColumnName("StatusDescription");
            builder.Property(t => t.ClientReceivedTimeStamp).HasColumnName("ClientReceivedTimeStamp");
            builder.Property(t => t.ServerReceivedTimeStamp).HasColumnName("ServerReceivedTimeStamp");
            builder.Property(t => t.ServiceRequestId).HasColumnName("ServiceRequestId");

            // Relationships
            builder.HasOne(t => t.ServiceRequest)
                .WithMany(t => t.Events)
                .HasForeignKey(d => d.ServiceRequestId)
                .IsRequired(false);
        }
    }
}
