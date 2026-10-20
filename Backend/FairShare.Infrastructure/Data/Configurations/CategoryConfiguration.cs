using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FairShare.Infrastructure.Data.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(Category.MaxNameLength);

            // CategoryService checks for duplicate names; the unique index guarantees it even
            // when two requests create the same category at the same moment. A rejected proposal
            // does not count, so the same name can be proposed again later.
            builder.HasIndex(c => c.Name)
                    .IsUnique()
                    .HasFilter($"\"Status\" <> {(int)CategoryStatus.Rejected}");

            builder.Property(c => c.Icon)
                    .HasMaxLength(50);

            // A new NOT NULL column gets 0 for the rows that already exist, and 0 is Approved:
            // the categories from before this change stay usable.
            builder.Property(c => c.Status)
                    .IsRequired();

            // The administrator's list: proposals waiting for a decision.
            builder.HasIndex(c => c.Status);

            builder.Property(c => c.RejectionReason)
                    .HasMaxLength(Category.MaxRejectionReasonLength);

            // A proposal stays even if the account that proposed it is removed.
            builder.HasOne(c => c.ProposedByUser)
                    .WithMany()
                    .HasForeignKey(c => c.ProposedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
