using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class PendingPasswordResetMap : IEntityTypeConfiguration<PendingPasswordReset>
    {
        public void Configure(EntityTypeBuilder<PendingPasswordReset> builder)
        {
            builder.HasKey(t => t.PendingPasswordResetId);

            builder.Property(t => t.Email).IsRequired().HasMaxLength(256);
            builder.Property(t => t.Token).IsRequired().HasMaxLength(128);
            builder.Property(t => t.CreatedAt).IsRequired();
            builder.Property(t => t.ExpiresAt).IsRequired();

            builder.ToTable("PendingPasswordReset");

            builder.HasIndex(t => t.Token).IsUnique();
            builder.HasIndex(t => t.Email);
        }
    }
}
