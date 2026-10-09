namespace FairShare.Domain.Interfaces;

/// <summary>
/// Groups all repositories and saves their changes atomically in one transaction
/// (e.g. a group expense, its splits and the removal of outdated settlement suggestions).
/// </summary>
public interface IUnitOfWork
{
    IUserRepository Users { get; }
    ICategoryRepository Categories { get; }
    IExpenseRepository Expenses { get; }
    IBudgetRepository Budgets { get; }
    IGroupRepository Groups { get; }
    IGroupMemberRepository GroupMembers { get; }
    IGroupExpenseRepository GroupExpenses { get; }
    IExpenseSplitRepository ExpenseSplits { get; }
    ICommentRepository Comments { get; }
    ISettlementTransactionRepository SettlementTransactions { get; }
    IQrPaymentDataRepository QrPaymentData { get; }
    IPaymentCardRepository PaymentCards { get; }
    IPaymentRepository Payments { get; }
    INotificationRepository Notifications { get; }
    IAuditLogRepository AuditLogs { get; }

    // NEW: activation, password reset and refresh tokens
    IUserTokenRepository UserTokens { get; }

    /// <summary>Saves all pending changes in one transaction and returns the number of affected rows.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
