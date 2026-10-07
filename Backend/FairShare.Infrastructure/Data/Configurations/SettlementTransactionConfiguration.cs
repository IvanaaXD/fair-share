using FairShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FairShare.Infrastructure.Data.Configurations
{
    public class SettlementTransactionConfiguration : IEntityTypeConfiguration<SettlementTransaction>
    {
        public void Configure(EntityTypeBuilder<SettlementTransaction> builder)
        {
            builder.Property(s => s.Amount)
                    .IsRequired()
                    .HasPrecision(18, 2);

            builder.Property(s => s.Status)
                    .IsRequired();

            builder.Property(s => s.CreatedAt)
                    .IsRequired();

            // NEW: optimistic concurrency. A uint row-version property is mapped by Npgsql to the
            // PostgreSQL system column "xmin" - no real column is added to the table. EF adds
            // "WHERE xmin = <value read earlier>" to every UPDATE/DELETE of this entity and throws
            // DbUpdateConcurrencyException if the row was changed in the meantime.
            builder.Property(s => s.Version)
                    .IsRowVersion();

            // Balances and suggestions read settlements of one group filtered by status.
            builder.HasIndex(s => new { s.GroupId, s.Status });

            builder.HasOne(s => s.Group)
                    .WithMany(g => g.Settlements)
                    .HasForeignKey(s => s.GroupId)
                    .OnDelete(DeleteBehavior.Restrict);

            // Two relationships to the same User table (debtor and creditor) -
            // both Restrict, otherwise PostgreSQL would get multiple cascade paths.
            builder.HasOne(s => s.DebtorUser)
                    .WithMany(u => u.SettlementsAsDebtor)
                    .HasForeignKey(s => s.DebtorUserId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.CreditorUser)
                    .WithMany(u => u.SettlementsAsCreditor)
                    .HasForeignKey(s => s.CreditorUserId)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
