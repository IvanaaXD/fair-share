using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FairShare.API.Hubs;

/// <summary>
/// Real-time channel for notifications (SignalR). The server only sends; the client does not
/// call any hub method. Every signed-in user receives only their own messages - SignalR groups
/// connections by user id (see NameIdentifierUserIdProvider), so a phone and two browser tabs
/// of the same user all get them.
///
/// Messages sent to the client:
///   "notificationCreated" (notification, unreadCount)
///   "unreadCountChanged"  (unreadCount)
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    /// <summary>The address the web and mobile apps connect to (behind /api, like the rest of the API).</summary>
    public const string Path = "/api/hubs/notifications";
}
