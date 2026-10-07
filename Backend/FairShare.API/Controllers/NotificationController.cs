using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Notifications;
using FairShare.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUser;

    public NotificationController(INotificationService notificationService, ICurrentUserService currentUser)
    {
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    /// <summary>Обавјештења тренутног корисника; isRead=false враћа само непрочитана.</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<NotificationResponse>>> GetMine(
        [FromQuery] bool? isRead,
        CancellationToken cancellationToken)
    {
        var result = await _notificationService.GetMyNotificationsAsync(_currentUser.UserId, isRead, cancellationToken);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await _notificationService.GetUnreadCountAsync(_currentUser.UserId, cancellationToken);
        return Ok(new { count });
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead([FromRoute] Guid notificationId, CancellationToken cancellationToken)
    {
        await _notificationService.MarkAsReadAsync(notificationId, _currentUser.UserId, cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllAsReadAsync(_currentUser.UserId, cancellationToken);
        return NoContent();
    }
}
