using FairShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FairShare.Infrastructure.Data.Configurations
{
    public class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
    {
        public void Configure(EntityTypeBuilder<UserToken> builder)
        {
            // Explicit table name, so it does not depend on whether a DbSet is declared in the context.
            builder.ToTable("UserTokens");

            builder.Property(t => t.Type)
                    .IsRequired();

            // SHA-256 written as hex is always 64 characters.
            builder.Property(t => t.TokenHash)
                    .IsRequired()
                    .HasMaxLength(64);

            // Tokens are looked up by hash on every refresh / activation / reset.
            builder.HasIndex(t => t.TokenHash)
                    .IsUnique();

            builder.Property(t => t.CreatedAt)
                    .IsRequired();

            builder.Property(t => t.ExpiresAt)
                    .IsRequired();

            // Mapped to the PostgreSQL system column "xmin" - guarantees a token is used only once
            // even if two requests arrive at the same moment.
            builder.Property(t => t.Version)
                    .IsRowVersion();

            // "All usable tokens of this user and type" - used when revoking tokens.
            builder.HasIndex(t => new { t.UserId, t.Type });

            // Tokens have no meaning without their user.
            builder.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
