using FairShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FairShare.Infrastructure.Data.Configurations
{
    public class PaymentCardConfiguration : IEntityTypeConfiguration<PaymentCard>
    {
        public void Configure(EntityTypeBuilder<PaymentCard> builder)
        {
            // Only the token issued by the payment simulator is stored - never the card number or CVV (PCI-DSS).
            builder.Property(c => c.Token)
                    .IsRequired()
                    .HasMaxLength(100);

            builder.HasIndex(c => c.Token)
                    .IsUnique();

            builder.Property(c => c.Last4Digits)
                    .IsRequired()
                    .HasMaxLength(4);

            builder.Property(c => c.Brand)
                    .IsRequired()
                    .HasMaxLength(30);

            builder.Property(c => c.ExpiryDate)
                    .IsRequired();

            builder.HasOne(c => c.User)
                    .WithMany(u => u.PaymentCards)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
