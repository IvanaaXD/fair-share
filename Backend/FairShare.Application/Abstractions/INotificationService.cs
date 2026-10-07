using FairShare.Application.DTOs.Notifications;
using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Чува обавјештење у бази и ставља e-mail у ред за слање. Позвати НАКОН
    /// SaveChangesAsync у сервису који је изазвао догађај.
    /// </summary>
    Task NotifyAsync(
        Guid userId,
        NotificationType type,
        string subject,
        string message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationResponse>> GetMyNotificationsAsync(
        Guid currentUserId,
        bool? isRead,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid currentUserId, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid notificationId, Guid currentUserId, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(Guid currentUserId, CancellationToken cancellationToken = default);
}
