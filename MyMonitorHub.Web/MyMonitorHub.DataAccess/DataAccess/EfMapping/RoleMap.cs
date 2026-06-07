using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class RoleMap : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            // Primary Key
            builder.HasKey(t => t.RoleId);

            // Properties
            builder.Property(t => t.RoleCode)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(50);

            // Table & Column Mappings
            builder.ToTable("Role");
            builder.Property(t => t.RoleId).HasColumnName("RoleId");
            builder.Property(t => t.AccountId).HasColumnName("AccountId");
            builder.Property(t => t.RoleCode).HasColumnName("RoleCode");
            builder.Property(t => t.RoleXML).HasColumnName("RoleXML");
            builder.Property(t => t.Description).HasColumnName("Description");
        }
    }
}
