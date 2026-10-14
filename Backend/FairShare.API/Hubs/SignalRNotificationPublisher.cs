using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace FairShare.API.Hubs;

/// <summary>
/// INotificationPublisher over SignalR. A failed push is only logged: the notification is
/// already in the database, and the app loads it the next time it asks for the list.
/// </summary>
public class SignalRNotificationPublisher : INotificationPublisher
{
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ILogger<SignalRNotificationPublisher> _logger;

    public SignalRNotificationPublisher(IHubContext<NotificationHub> hub, ILogger<SignalRNotificationPublisher> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task NotificationCreatedAsync(
        Guid userId,
        NotificationResponse notification,
        int unreadCount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _hub.Clients.User(userId.ToString())
                .SendAsync("notificationCreated", notification, unreadCount, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Real-time notification for user {UserId} was not sent.", userId);
        }
    }

    public async Task UnreadCountChangedAsync(Guid userId, int unreadCount, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hub.Clients.User(userId.ToString())
                .SendAsync("unreadCountChanged", unreadCount, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Unread count for user {UserId} was not sent.", userId);
        }
    }
}
