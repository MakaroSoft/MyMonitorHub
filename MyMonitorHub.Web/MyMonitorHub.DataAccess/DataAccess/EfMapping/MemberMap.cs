using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class MemberMap : IEntityTypeConfiguration<Member>
    {
        public void Configure(EntityTypeBuilder<Member> builder)
        {
            // Primary Key
            builder.HasKey(t => t.MemberId);

            // Properties
            builder.Property(t => t.SendAlertsTo)
                .HasMaxLength(50);

            // Table & Column Mappings
            builder.ToTable("Member");
            builder.Property(t => t.MemberId).HasColumnName("MemberId");
            builder.Property(t => t.OrganizationId).HasColumnName("OrganizationId");
            builder.Property(t => t.UserId).HasColumnName("UserId");
            builder.Property(t => t.RoleId).HasColumnName("RoleId");
            builder.Property(t => t.SendAlertsTo).HasColumnName("SendAlertsTo");
            builder.Property(t => t.DefaultOrganization).HasColumnName("DefaultOrganization");

            // Relationships
            builder.HasOne(t => t.Account)
                .WithMany(t => t.Members)
                .HasForeignKey(d => d.OrganizationId);
            builder.HasOne(t => t.Role)
                .WithMany(t => t.Members)
                .HasForeignKey(d => d.RoleId);
            builder.HasOne(t => t.User)
                .WithMany(t => t.Members)
                .HasForeignKey(d => d.UserId);
        }
    }
}
