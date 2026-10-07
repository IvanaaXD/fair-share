using FairShare.Domain.Entities;
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
                    .HasMaxLength(50);

            // CategoryService checks for duplicate names; the unique index guarantees it
            // even when two requests create the same category at the same moment.
            builder.HasIndex(c => c.Name)
                    .IsUnique();

            builder.Property(c => c.Icon)
                    .HasMaxLength(50);
        }
    }
}
