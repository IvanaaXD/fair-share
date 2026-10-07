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
