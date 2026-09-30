using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class CommentRepository : GenericRepository<Comment>, ICommentRepository
{
    public CommentRepository(FairShareDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Comment>> GetByGroupExpenseAsync(
        Guid groupExpenseId,
        CancellationToken cancellationToken = default)
        => await DbSet.AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.GroupExpenseId == groupExpenseId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
}
