using FairShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FairShare.Infrastructure.Data.Configurations
{
    public class ExpenseSplitConfiguration : IEntityTypeConfiguration<ExpenseSplit>
    {
        public void Configure(EntityTypeBuilder<ExpenseSplit> builder)
        {
            builder.Property(es => es.Amount)
                    .IsRequired()
                    .HasPrecision(18, 2);

            // Filled only for percentage splits.
            builder.Property(es => es.Percentage);

            // Splits live and die with their expense.
            builder.HasOne(es => es.GroupExpense)
                    .WithMany(ge => ge.Splits)
                    .HasForeignKey(es => es.GroupExpenseId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(es => es.User)
                    .WithMany(u => u.ExpenseSplits)
                    .HasForeignKey(es => es.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
