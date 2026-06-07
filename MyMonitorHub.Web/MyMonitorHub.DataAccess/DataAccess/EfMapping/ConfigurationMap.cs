using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class ConfigurationMap : IEntityTypeConfiguration<Configuration>
    {
        public void Configure(EntityTypeBuilder<Configuration> builder)
        {
            // Primary Key
            builder.HasKey(t => t.configurationId);

            // Table & Column Mappings
            builder.ToTable("Configuration");
            builder.Property(t => t.configurationId).HasColumnName("configurationId");
            builder.Property(t => t.accountId).HasColumnName("accountId");
            builder.Property(t => t.healthResponseMinutes).HasColumnName("healthResponseMinutes");
            builder.Property(t => t.healthResponseAlertMinutes).HasColumnName("healthResponseAlertMinutes");
            builder.Property(t => t.srNotAcceptedAlertMinutes).HasColumnName("srNotAcceptedAlertMinutes");

            // Relationships
            builder.HasOne(t => t.Account)
                .WithMany(t => t.Configurations)
                .HasForeignKey(d => d.accountId);
        }
    }
}
