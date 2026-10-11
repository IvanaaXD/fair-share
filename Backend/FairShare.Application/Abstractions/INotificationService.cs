using FairShare.Application.DTOs.Notifications;
using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Saves the notification in the database and enqueues the e-mail for sending. Call AFTER
    /// SaveChangesAsync in the service that triggered the event.
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
