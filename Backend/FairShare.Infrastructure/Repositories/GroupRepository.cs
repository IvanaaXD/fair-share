using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class GroupRepository : GenericRepository<Group>, IGroupRepository
{
    public GroupRepository(FairShareDbContext context) : base(context)
    {
    }

    public async Task<Group?> GetWithMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
        => await DbSet
            .Include(g => g.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

    public async Task<IReadOnlyList<Group>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Where(g => g.Members.Any(m => m.UserId == userId))
            .ToListAsync(cancellationToken);
}
