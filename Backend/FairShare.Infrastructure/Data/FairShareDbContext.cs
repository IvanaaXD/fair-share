using FairShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Data;

public class FairShareDbContext : DbContext
{
    public FairShareDbContext(DbContextOptions<FairShareDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<GroupExpense> GroupExpenses { get; set; }
    public DbSet<ExpenseSplit> ExpenseSplits { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<SettlementTransaction> SettlementTransactions { get; set; }
    public DbSet<PaymentCard> PaymentCards { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<QrPaymentData> QrPaymentData { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SettlementTransaction>()
                .HasOne(st => st.CreditorUser)
                .WithMany()
                .HasForeignKey(st => st.CreditorUserId)
                .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SettlementTransaction>()
            .HasOne(st => st.DebtorUser)
            .WithMany()
            .HasForeignKey(st => st.DebtorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}