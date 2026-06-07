using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class AccountMap : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            // Primary Key
            builder.HasKey(t => t.AccountId);

            // Properties
            builder.Property(t => t.Identification)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(50);

            // Table & Column Mappings
            builder.ToTable("Account");
            builder.Property(t => t.AccountId).HasColumnName("AccountId");
            builder.Property(t => t.Identification).HasColumnName("Identification");
            builder.Property(t => t.Description).HasColumnName("Description");
            builder.Property(t => t.Rules).HasColumnName("Rules");
        }
    }
}
