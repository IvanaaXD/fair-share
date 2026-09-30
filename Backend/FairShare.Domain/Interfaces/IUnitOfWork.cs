namespace FairShare.Domain.Interfaces;

/// <summary>
/// Обједињује све репозиторијуме и обезбјеђује атомарно чување измјена у оквиру
/// једне трансакције (нпр. истовремена измјена трошка, подјеле и салда групе -
/// функционалност 5.6).
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

    /// <summary>Чува све измјене унутар текуће трансакције и враћа број измијењених записа.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
