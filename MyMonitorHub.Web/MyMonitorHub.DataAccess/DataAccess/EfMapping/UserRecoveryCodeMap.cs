using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class UserRecoveryCodeMap : IEntityTypeConfiguration<UserRecoveryCode>
    {
        public void Configure(EntityTypeBuilder<UserRecoveryCode> builder)
        {
            builder.HasKey(t => t.UserRecoveryCodeId);
            builder.ToTable("UserRecoveryCodes");
            builder.Property(t => t.UserId).IsRequired();
            builder.Property(t => t.CodeHash).IsRequired().HasMaxLength(128);
            builder.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .IsRequired();
            builder.HasIndex(t => t.UserId);
            builder.HasIndex(t => t.CodeHash).IsUnique();
        }
    }
}
