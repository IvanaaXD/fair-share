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
            .Include(ge => ge.Splits).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(ge => ge.Id == groupExpenseId, cancellationToken);

    // NEW: tracked (no AsNoTracking), because the caller edits or deletes the expense.
    public async Task<GroupExpense?> GetWithDetailsAsync(Guid groupExpenseId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(ge => ge.Category)
            .Include(ge => ge.PaidByUser)
            .Include(ge => ge.Splits).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(ge => ge.Id == groupExpenseId, cancellationToken);

    public async Task<IReadOnlyList<GroupExpense>> GetByGroupAsync(
        Guid groupId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(ge => ge.Category)
            .Include(ge => ge.PaidByUser)
            .Include(ge => ge.Splits).ThenInclude(s => s.User)
            .Where(ge => ge.GroupId == groupId)
            .OrderByDescending(ge => ge.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<GroupExpense>> GetAllByGroupAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(ge => ge.Splits)
            .Where(ge => ge.GroupId == groupId)
            .ToListAsync(cancellationToken);
}
