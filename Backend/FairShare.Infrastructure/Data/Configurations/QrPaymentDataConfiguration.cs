using FairShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FairShare.Infrastructure.Data.Configurations
{
    public class QrPaymentDataConfiguration : IEntityTypeConfiguration<QrPaymentData>
    {
        public void Configure(EntityTypeBuilder<QrPaymentData> builder)
        {
            builder.Property(q => q.Amount)
                    .IsRequired()
                    .HasPrecision(18, 2);

            // Empty until the creditor enters a bank account in their profile.
            builder.Property(q => q.RecipientAccount)
                    .IsRequired()
                    .HasMaxLength(18);

            builder.Property(q => q.Currency)
                    .IsRequired()
                    .HasMaxLength(3);

            // IPS "RO" field allows at most 35 characters.
            builder.Property(q => q.ReferenceCode)
                    .IsRequired()
                    .HasMaxLength(35);

            // One-to-one: the foreign key lives on QrPaymentData.
            builder.HasOne(q => q.SettlementTransaction)
                    .WithOne(s => s.QrPaymentData)
                    .HasForeignKey<QrPaymentData>(q => q.SettlementTransactionId)
                    .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
