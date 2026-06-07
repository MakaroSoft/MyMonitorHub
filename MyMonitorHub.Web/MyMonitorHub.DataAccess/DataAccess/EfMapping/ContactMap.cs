using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMonitorHub.Domain.Entity;

namespace MyMonitorHub.DataAccess.EfMapping
{
    public class ContactMap : IEntityTypeConfiguration<Contact>
    {
        public void Configure(EntityTypeBuilder<Contact> builder)
        {
            builder.Property(e => e.Name)
                .IsUnicode(false);

            builder.Property(e => e.Phone1)
                .IsUnicode(false);

            builder.Property(e => e.Phone2)
                .IsUnicode(false);

            builder.Property(e => e.Phone3)
                .IsUnicode(false);

            builder.Property(e => e.Notes)
                .IsUnicode(false);

            builder.HasOne(m => m.PhoneType1)
                .WithMany(t => t.Contacts1)
                .HasForeignKey(m => m.PhoneType1Id)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.PhoneType2)
                .WithMany(t => t.Contacts2)
                .HasForeignKey(m => m.PhoneType2Id)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.PhoneType3)
                .WithMany(t => t.Contacts3)
                .HasForeignKey(m => m.PhoneType3Id)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
