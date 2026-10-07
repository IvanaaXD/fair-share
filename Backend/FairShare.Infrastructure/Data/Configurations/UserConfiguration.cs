using FairShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FairShare.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(u => u.FirstName)
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(u => u.LastName)
                    .IsRequired()
                    .HasMaxLength(100);

            builder.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(256);

            // Login looks users up by e-mail, and an e-mail can belong to one account only.
            builder.HasIndex(u => u.Email)
                    .IsUnique();

            // BCrypt hashes are 60 characters; extra room allows switching algorithms later.
            builder.Property(u => u.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255);

            builder.Property(u => u.ProfileImageUrl)
                    .HasMaxLength(500);

            builder.Property(u => u.DefaultCurrency)
                    .IsRequired()
                    .HasMaxLength(3);

            // Digits only: 16 (BiH) or 18 (Serbia).
            builder.Property(u => u.BankAccountNumber)
                    .HasMaxLength(18);

            // Relationships are configured on the dependent side (the entity holding the foreign key).
        }
    }
}
