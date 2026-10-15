using FairShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FairShare.Infrastructure.Data.Configurations
{
    public class ProcessedWebhookEventConfiguration : IEntityTypeConfiguration<ProcessedWebhookEvent>
    {
        public void Configure(EntityTypeBuilder<ProcessedWebhookEvent> builder)
        {
            // Webhook idempotency: the same event can be stored only once, even if two copies
            // of it arrive at the same moment.
            builder.Property(e => e.EventId)
                    .IsRequired()
                    .HasMaxLength(64);

            builder.HasIndex(e => e.EventId)
                    .IsUnique();

            builder.Property(e => e.Type)
                    .IsRequired()
                    .HasMaxLength(50);

            builder.Property(e => e.ChargeId)
                    .IsRequired()
                    .HasMaxLength(64);

            builder.Property(e => e.CreatedAt)
                    .IsRequired();

            // Only a reference for tracing; the payment may be deleted together with its settlement.
            builder.HasIndex(e => e.PaymentId);
        }
    }
}
