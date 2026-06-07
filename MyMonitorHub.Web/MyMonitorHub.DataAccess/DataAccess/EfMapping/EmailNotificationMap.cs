using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class EmailNotificationMap : IEntityTypeConfiguration<EmailNotification>
    {
        public void Configure(EntityTypeBuilder<EmailNotification> builder)
        {
            // Primary Key
            builder.HasKey(t => t.EmailNotificationId);

            // Table & Column Mappings
            builder.ToTable("EmailNotification");
            builder.Property(t => t.EmailNotificationId).HasColumnName("EmailNotificationId");
            builder.Property(t => t.UserId).HasColumnName("UserId");
            builder.Property(t => t.Email).HasColumnName("Email");
            builder.Property(t => t.Disabled).HasColumnName("Disabled");

            // Relationships
            builder.HasOne(t => t.User)
                .WithMany(t => t.EmailNotifications)
                .HasForeignKey(d => d.UserId);
        }
    }
}
