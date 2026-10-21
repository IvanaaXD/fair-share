using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    /// <summary>How many times a save is repeated after an outdated suggestion was already deleted by someone else.</summary>
    private const int MaxSaveAttempts = 5;

    private readonly FairShareDbContext _context;

    public UnitOfWork(FairShareDbContext context)
    {
        _context = context;

        // Every repository works on the same DbContext, so one SaveChangesAsync call
        // saves the changes of all of them in a single transaction.
        Users = new UserRepository(context);
        Categories = new CategoryRepository(context);
        Expenses = new ExpenseRepository(context);
        Budgets = new BudgetRepository(context);
        Groups = new GroupRepository(context);
        GroupMembers = new GroupMemberRepository(context);
        GroupExpenses = new GroupExpenseRepository(context);
        ExpenseSplits = new ExpenseSplitRepository(context);
        Comments = new CommentRepository(context);
        SettlementTransactions = new SettlementTransactionRepository(context);
        QrPaymentData = new QrPaymentDataRepository(context);
        PaymentCards = new PaymentCardRepository(context);
        Payments = new PaymentRepository(context);
        Notifications = new NotificationRepository(context);
        AuditLogs = new AuditLogRepository(context);
        UserTokens = new UserTokenRepository(context);
        ProcessedWebhookEvents = new ProcessedWebhookEventRepository(context);
    }

    public IUserRepository Users { get; }
    public ICategoryRepository Categories { get; }
    public IExpenseRepository Expenses { get; }
    public IBudgetRepository Budgets { get; }
    public IGroupRepository Groups { get; }
    public IGroupMemberRepository GroupMembers { get; }
    public IGroupExpenseRepository GroupExpenses { get; }
    public IExpenseSplitRepository ExpenseSplits { get; }
    public ICommentRepository Comments { get; }
    public ISettlementTransactionRepository SettlementTransactions { get; }
    public IQrPaymentDataRepository QrPaymentData { get; }
    public IPaymentCardRepository PaymentCards { get; }
    public IPaymentRepository Payments { get; }
    public INotificationRepository Notifications { get; }
    public IAuditLogRepository AuditLogs { get; }
    public IUserTokenRepository UserTokens { get; }
    public IProcessedWebhookEventRepository ProcessedWebhookEvents { get; }

    /// <summary>
    /// Saves all pending changes in one transaction. One kind of concurrency conflict is resolved
    /// here instead of being reported to the user: two members add an expense to the same group
    /// at the same moment and both remove the group's outdated settlement suggestions. The
    /// second one finds them already deleted - which is exactly what it wanted - so its save is
    /// repeated without them. Every other conflict (e.g. two edits of the same expense) still
    /// ends with DbUpdateConcurrencyException, i.e. HTTP 409.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException conflict) when (attempt < MaxSaveAttempts)
            {
                // The failed transaction was rolled back and the tracked changes are kept,
                // so after resolving the conflict the same changes are simply saved again.
                if (!await ResolveOutdatedSuggestionDeletesAsync(conflict, cancellationToken))
                    throw;

                // A short random pause, so requests that collided do not collide again at once.
                await Task.Delay(Random.Shared.Next(5, 20) * attempt, cancellationToken);
            }
        }
    }

    /// <summary>
    /// True if every conflicting row is a settlement suggestion this request wanted to delete.
    /// A suggestion that is already gone, or meanwhile became a real (paid) settlement, is left
    /// alone; one that is still a suggestion but was changed (e.g. a reminder was sent) is
    /// deleted again with its current version.
    /// </summary>
    private static async Task<bool> ResolveOutdatedSuggestionDeletesAsync(
        DbUpdateConcurrencyException conflict,
        CancellationToken cancellationToken)
    {
        if (conflict.Entries.Count == 0 ||
            conflict.Entries.Any(entry => entry.Entity is not SettlementTransaction || entry.State != EntityState.Deleted))
            return false;

        foreach (var entry in conflict.Entries)
        {
            var current = await entry.GetDatabaseValuesAsync(cancellationToken);
            var stillProposed = current is not null &&
                                current.GetValue<SettlementStatus>(nameof(SettlementTransaction.Status)) == SettlementStatus.Proposed;

            if (stillProposed)
                entry.OriginalValues.SetValues(current!);
            else
                entry.State = EntityState.Detached;
        }

        return true;
    }
}
