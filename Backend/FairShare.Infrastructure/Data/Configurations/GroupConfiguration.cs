using FairShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FairShare.Infrastructure.Data.Configurations
{
    public class GroupConfiguration : IEntityTypeConfiguration<Group>
    {
        public void Configure(EntityTypeBuilder<Group> builder)
        {
            builder.Property(g => g.Name)
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(g => g.Description)
                    .HasMaxLength(500);

            builder.Property(g => g.Currency)
                    .IsRequired()
                    .HasMaxLength(3);

            builder.Property(g => g.CreatedAt)
                    .IsRequired();
        }
    }
}
