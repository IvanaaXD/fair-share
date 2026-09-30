using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class ExpenseSplitRepository : GenericRepository<ExpenseSplit>, IExpenseSplitRepository
{
    public ExpenseSplitRepository(FairShareDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ExpenseSplit>> GetByGroupExpenseAsync(
        Guid groupExpenseId,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(s => s.GroupExpenseId == groupExpenseId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ExpenseSplit>> GetByGroupAndUserAsync(
        Guid groupId,
        Guid userId,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(s => s.UserId == userId && s.GroupExpense.GroupId == groupId)
            .ToListAsync(cancellationToken);
}
