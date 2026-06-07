using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class PhoneTypeMap : IEntityTypeConfiguration<PhoneType>
    {
        public void Configure(EntityTypeBuilder<PhoneType> builder)
        {
            // Primary Key
            builder.HasKey(t => t.PhoneTypeId);

            // Table & Column Mappings
            builder.ToTable("PhoneType");

            builder.Property(e => e.Name)
                .IsUnicode(false);
        }
    }
}
