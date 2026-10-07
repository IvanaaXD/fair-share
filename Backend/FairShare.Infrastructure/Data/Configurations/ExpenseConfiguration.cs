using FairShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FairShare.Infrastructure.Data.Configurations
{
    public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
    {
        public void Configure(EntityTypeBuilder<Expense> builder)
        {
            // Money is always decimal, never float/double.
            builder.Property(e => e.Amount)
                    .IsRequired()
                    .HasPrecision(18, 2);

            builder.Property(e => e.Currency)
                    .IsRequired()
                    .HasMaxLength(3);

            builder.Property(e => e.Date)
                    .IsRequired();

            builder.Property(e => e.Description)
                    .HasMaxLength(500);

            builder.Property(e => e.ReceiptImageUrl)
                    .HasMaxLength(500);

            // Expense list, budgets and analytics all filter by user and date range.
            builder.HasIndex(e => new { e.UserId, e.Date });

            // An expense belongs to its user - deleting the user deletes their expenses.
            builder.HasOne(e => e.User)
                    .WithMany(u => u.Expenses)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

            // Restrict: a category in use cannot be deleted, so history is never lost.
            builder.HasOne(e => e.Category)
                    .WithMany(c => c.Expenses)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
