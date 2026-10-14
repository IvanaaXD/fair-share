using FairShare.Application.DTOs.Notifications;

namespace FairShare.Application.Abstractions;

/// <summary>
/// Pushes notifications to the user's open apps in real time (SignalR in the API layer).
/// The Application layer only knows this interface, not SignalR itself. Implementations must
/// never throw: the notification is already saved, and a user who is offline simply sees it
/// the next time the app loads the list.
/// </summary>
public interface INotificationPublisher
{
    /// <summary>A new notification, together with the new number of unread ones (for the badge).</summary>
    Task NotificationCreatedAsync(
        Guid userId,
        NotificationResponse notification,
        int unreadCount,
        CancellationToken cancellationToken = default);

    /// <summary>Notifications were read (perhaps in another tab or on the phone) - only the badge changes.</summary>
    Task UnreadCountChangedAsync(Guid userId, int unreadCount, CancellationToken cancellationToken = default);
}
