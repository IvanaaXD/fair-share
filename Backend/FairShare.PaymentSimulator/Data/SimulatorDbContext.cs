using FairShare.PaymentSimulator.Domain;
using Microsoft.EntityFrameworkCore;

namespace FairShare.PaymentSimulator.Data;

/// <summary>
/// The simulator's own database (fairshare_payments). FairShare's database never sees these
/// tables, and the simulator never sees FairShare's - the two services share only HTTP.
/// </summary>
public class SimulatorDbContext : DbContext
{
    public SimulatorDbContext(DbContextOptions<SimulatorDbContext> options) : base(options)
    {
    }

    public DbSet<CardToken> CardTokens => Set<CardToken>();
    public DbSet<Charge> Charges => Set<Charge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CardToken>(card =>
        {
            card.HasKey(c => c.Id);
            card.Property(c => c.Token).IsRequired().HasMaxLength(64);
            card.HasIndex(c => c.Token).IsUnique();
            card.Property(c => c.Last4Digits).IsRequired().HasMaxLength(4);
            card.Property(c => c.Brand).IsRequired().HasMaxLength(30);
            card.Property(c => c.HolderName).HasMaxLength(100);
            card.Property(c => c.Outcome).IsRequired().HasMaxLength(30);
        });

        modelBuilder.Entity<Charge>(charge =>
        {
            charge.HasKey(c => c.Id);
            charge.Property(c => c.Id).HasMaxLength(64);

            // The real guarantee against double charges: a second row with the same key cannot exist.
            charge.Property(c => c.IdempotencyKey).IsRequired().HasMaxLength(100);
            charge.HasIndex(c => c.IdempotencyKey).IsUnique();

            charge.Property(c => c.Amount).HasPrecision(18, 2);
            charge.Property(c => c.Currency).IsRequired().HasMaxLength(3);
            charge.Property(c => c.Reference).IsRequired().HasMaxLength(100);
            charge.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
            charge.Property(c => c.FailureCode).HasMaxLength(30);
            charge.Property(c => c.EventId).HasMaxLength(64);
            charge.Property(c => c.LastWebhookError).HasMaxLength(300);

            // The processor looks for pending charges and for undelivered webhooks.
            charge.HasIndex(c => new { c.Status, c.CreatedAt });
            charge.HasIndex(c => new { c.WebhookDeliveredAt, c.NextWebhookAttemptAt });

            charge.HasOne(c => c.CardToken)
                .WithMany()
                .HasForeignKey(c => c.CardTokenId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
