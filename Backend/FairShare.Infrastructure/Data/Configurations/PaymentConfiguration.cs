using FairShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FairShare.Infrastructure.Data.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            // The unique index is what really prevents double payments: a retried request
            // with the same idempotency key cannot insert a second row.
            builder.Property(p => p.IdempotencyKey)
                    .IsRequired()
                    .HasMaxLength(100);

            builder.HasIndex(p => p.IdempotencyKey)
                    .IsUnique();

            builder.Property(p => p.Status)
                    .IsRequired();

            builder.Property(p => p.CreatedAt)
                    .IsRequired();

            // One-to-zero-or-one: a settlement can be paid by card at most once.
            builder.HasOne(p => p.SettlementTransaction)
                    .WithOne(s => s.Payment)
                    .HasForeignKey<Payment>(p => p.SettlementTransactionId)
                    .OnDelete(DeleteBehavior.Cascade);

            // Restrict: a card that was used for payments cannot be deleted without
            // handling its payment history first.
            builder.HasOne(p => p.PaymentCard)
                    .WithMany(c => c.Payments)
                    .HasForeignKey(p => p.PaymentCardId)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
