using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class DeviceRefreshTokenMap : IEntityTypeConfiguration<DeviceRefreshToken>
    {
        public void Configure(EntityTypeBuilder<DeviceRefreshToken> builder)
        {
            builder.HasKey(t => t.Id);

            builder.ToTable("DeviceRefreshTokens");

            builder.Property(t => t.Id).HasColumnName("Id");
            builder.Property(t => t.DeviceId).HasColumnName("DeviceId");
            builder.Property(t => t.AccountId).HasColumnName("AccountId");
            builder.Property(t => t.TokenHash).HasColumnName("TokenHash").HasMaxLength(64).IsRequired();
            builder.Property(t => t.FamilyId).HasColumnName("FamilyId");
            builder.Property(t => t.IssuedAt).HasColumnName("IssuedAt");
            builder.Property(t => t.ExpiresAt).HasColumnName("ExpiresAt");
            builder.Property(t => t.Revoked).HasColumnName("Revoked");

            builder.HasOne(t => t.Device)
                .WithMany()
                .HasForeignKey(t => t.DeviceId)
                .IsRequired();

            builder.HasIndex(t => t.TokenHash);
            builder.HasIndex(t => t.DeviceId);
        }
    }
}
