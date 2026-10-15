using FairShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FairShare.Infrastructure.Data.Configurations
{
    public class GroupExpenseConfiguration : IEntityTypeConfiguration<GroupExpense>
    {
        public void Configure(EntityTypeBuilder<GroupExpense> builder)
        {
            builder.Property(ge => ge.Amount)
                    .IsRequired()
                    .HasPrecision(18, 2);

            builder.Property(ge => ge.Description)
                    .HasMaxLength(500);

            // Address of the receipt photo (set by ReceiptService after an upload).
            builder.Property(ge => ge.ReceiptImageUrl)
                    .HasMaxLength(500);

            // An expense entered in another currency keeps what was actually paid and the rate
            // used; Amount is always in the group's currency. All four are null otherwise.
            builder.Property(ge => ge.OriginalCurrency)
                    .HasMaxLength(3);

            builder.Property(ge => ge.OriginalAmount)
                    .HasPrecision(18, 2);

            // Units of the group currency per one unit of the original currency, e.g. 117.17 RSD per EUR.
            builder.Property(ge => ge.ExchangeRate)
                    .HasPrecision(18, 8);

            builder.Property(ge => ge.Date)
                    .IsRequired();

            builder.Property(ge => ge.SplitType)
                    .IsRequired();

            // Group expense list is paged by group, newest first.
            builder.HasIndex(ge => new { ge.GroupId, ge.Date });

            builder.HasOne(ge => ge.Group)
                    .WithMany(g => g.Expenses)
                    .HasForeignKey(ge => ge.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ge => ge.Category)
                    .WithMany(c => c.GroupExpenses)
                    .HasForeignKey(ge => ge.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

            // Restrict avoids multiple cascade paths (User -> GroupMember -> Group -> GroupExpense).
            builder.HasOne(ge => ge.PaidByUser)
                    .WithMany()
                    .HasForeignKey(ge => ge.PaidByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
