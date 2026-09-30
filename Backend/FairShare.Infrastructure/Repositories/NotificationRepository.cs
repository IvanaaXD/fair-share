using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FairShare.Infrastructure.Repositories;

public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    public NotificationRepository(FairShareDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Notification>> GetByUserAsync(
        Guid userId,
        bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(n => n.UserId == userId);

        if (isRead.HasValue)
            query = query.Where(n => n.IsRead == isRead.Value);

        return await query.OrderByDescending(n => n.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default)
        => await DbSet.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
}
