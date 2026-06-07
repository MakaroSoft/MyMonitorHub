using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class UserMap : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Primary Key
            builder.HasKey(t => t.UserId);

            // Properties
            builder.Property(t => t.Password)
                .HasMaxLength(256);

            builder.Property(t => t.FirstName)
                .HasMaxLength(50);

            builder.Property(t => t.LastName)
                .HasMaxLength(50);

            builder.Property(t => t.HomePhone)
                .HasMaxLength(50);

            builder.Property(t => t.WorkPhone)
                .HasMaxLength(50);

            builder.Property(t => t.CellPhone)
                .HasMaxLength(50);

            builder.Property(t => t.Email)
                .HasMaxLength(50);

            builder.Property(t => t.HomePage)
                .HasMaxLength(200);

            // Table & Column Mappings
            builder.ToTable("User");
            builder.Property(t => t.UserId).HasColumnName("UserId");
            builder.Property(t => t.Password).HasColumnName("Password");
            builder.Property(t => t.FirstName).HasColumnName("FirstName");
            builder.Property(t => t.LastName).HasColumnName("LastName");
            builder.Property(t => t.HomePhone).HasColumnName("HomePhone");
            builder.Property(t => t.WorkPhone).HasColumnName("WorkPhone");
            builder.Property(t => t.CellPhone).HasColumnName("CellPhone");
            builder.Property(t => t.Email).HasColumnName("Email");
            builder.Property(t => t.HomePage).HasColumnName("HomePage");
        }
    }
}
