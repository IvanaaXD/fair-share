using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;

namespace FairShare.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
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

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
