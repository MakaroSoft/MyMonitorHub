using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class ServiceRequestMap : IEntityTypeConfiguration<ServiceRequest>
    {
        public void Configure(EntityTypeBuilder<ServiceRequest> builder)
        {
            // Primary Key
            builder.HasKey(t => t.ServiceRequestId);

            // Properties
            builder.Property(t => t.Notes)
                .IsRequired();

            builder.Property(t => t.Status)
                .IsRequired()
                .HasMaxLength(1);

            // Table & Column Mappings
            builder.ToTable("ServiceRequest");
            builder.Property(t => t.ServiceRequestId).HasColumnName("ServiceRequestId");
            builder.Property(t => t.AccountId).HasColumnName("AccountId");
            builder.Property(t => t.DeviceId).HasColumnName("DeviceId");
            builder.Property(t => t.Notes).HasColumnName("Notes");
            builder.Property(t => t.AssignedToId).HasColumnName("AssignedToId");
            builder.Property(t => t.TimeStamp).HasColumnName("TimeStamp");
            builder.Property(t => t.Status).HasColumnName("Status");
            builder.Property(t => t.LastAlertTime).HasColumnName("LastAlertTime");

            // Relationships
            builder.HasOne(t => t.Device)
                .WithMany(t => t.ServiceRequests)
                .HasForeignKey(d => d.DeviceId);
            builder.HasOne(t => t.User)
                .WithMany(t => t.ServiceRequests)
                .HasForeignKey(d => d.AssignedToId)
                .IsRequired(false);
        }
    }
}
