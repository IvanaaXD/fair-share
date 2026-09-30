using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class GroupMemberRepository : GenericRepository<GroupMember>, IGroupMemberRepository
{
    public GroupMemberRepository(FairShareDbContext context) : base(context)
    {
    }

    public async Task<GroupMember?> GetAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<GroupMember>> GetByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.GroupId == groupId)
            .ToListAsync(cancellationToken);

    public async Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.AnyAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);
}
