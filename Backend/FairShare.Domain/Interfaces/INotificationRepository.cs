using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IReadOnlyList<Notification>> GetByUserAsync(
        Guid userId,
        bool? isRead = null,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken = default);
}
