using FairShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FairShare.Infrastructure.Data.Configurations
{
    public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
    {
        public void Configure(EntityTypeBuilder<Budget> builder)
        {
            builder.Property(b => b.MonthlyLimit)
                    .IsRequired()
                    .HasPrecision(18, 2);

            // Format "YYYY-MM".
            builder.Property(b => b.Month)
                    .IsRequired()
                    .HasMaxLength(7);

            // One budget per user, category and month - enforced by the database as well as the service.
            builder.HasIndex(b => new { b.UserId, b.CategoryId, b.Month })
                    .IsUnique();

            builder.HasOne(b => b.User)
                    .WithMany(u => u.Budgets)
                    .HasForeignKey(b => b.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(b => b.Category)
                    .WithMany(c => c.Budgets)
                    .HasForeignKey(b => b.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
