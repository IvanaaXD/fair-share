using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class GroupExpenseRepository : GenericRepository<GroupExpense>, IGroupExpenseRepository
{
    public GroupExpenseRepository(FairShareDbContext context) : base(context)
    {
    }

    public async Task<GroupExpense?> GetWithSplitsAsync(Guid groupExpenseId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(ge => ge.Splits)
            .FirstOrDefaultAsync(ge => ge.Id == groupExpenseId, cancellationToken);

    public async Task<IReadOnlyList<GroupExpense>> GetByGroupAsync(
        Guid groupId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(ge => ge.GroupId == groupId)
            .OrderByDescending(ge => ge.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
}
