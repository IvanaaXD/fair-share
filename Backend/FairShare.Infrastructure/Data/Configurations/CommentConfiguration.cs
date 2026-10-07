using FairShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FairShare.Infrastructure.Data.Configurations
{
    public class CommentConfiguration : IEntityTypeConfiguration<Comment>
    {
        public void Configure(EntityTypeBuilder<Comment> builder)
        {
            builder.Property(c => c.Text)
                    .IsRequired()
                    .HasMaxLength(1000);

            builder.Property(c => c.CreatedAt)
                    .IsRequired();

            builder.HasOne(c => c.GroupExpense)
                    .WithMany(ge => ge.Comments)
                    .HasForeignKey(c => c.GroupExpenseId)
                    .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.User)
                    .WithMany(u => u.Comments)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
