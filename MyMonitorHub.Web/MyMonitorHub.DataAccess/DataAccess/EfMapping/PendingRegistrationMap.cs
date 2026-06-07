using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class PendingRegistrationMap : IEntityTypeConfiguration<PendingRegistration>
    {
        public void Configure(EntityTypeBuilder<PendingRegistration> builder)
        {
            builder.HasKey(t => t.PendingRegistrationId);

            builder.Property(t => t.Email).IsRequired().HasMaxLength(256);
            builder.Property(t => t.Password).IsRequired().HasMaxLength(256);
            builder.Property(t => t.FirstName).IsRequired().HasMaxLength(50);
            builder.Property(t => t.LastName).IsRequired().HasMaxLength(50);
            builder.Property(t => t.Token).IsRequired().HasMaxLength(128);
            builder.Property(t => t.CreatedAt).IsRequired();
            builder.Property(t => t.ExpiresAt).IsRequired();

            builder.ToTable("PendingRegistration");

            builder.HasIndex(t => t.Token).IsUnique();
            builder.HasIndex(t => t.Email);
        }
    }
}
