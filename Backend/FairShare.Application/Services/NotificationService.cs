using AutoMapper;
using FairShare.Application.Abstractions;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.DTOs.Notifications;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;

namespace FairShare.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailQueue _emailQueue;
    private readonly IMapper _mapper;

    public NotificationService(IUnitOfWork unitOfWork, IEmailQueue emailQueue, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _emailQueue = emailQueue;
        _mapper = mapper;
    }

    public async Task NotifyAsync(
        Guid userId,
        NotificationType type,
        string subject,
        string message,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            return;

        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // The e-mail is sent in the background; a failed send does not undo the in-app notification.
        await _emailQueue.EnqueueAsync(
            new EmailMessage(user.Email, $"{user.FirstName} {user.LastName}", subject, message),
            cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationResponse>> GetMyNotificationsAsync(
        Guid currentUserId,
        bool? isRead,
        CancellationToken cancellationToken = default)
    {
        var notifications = await _unitOfWork.Notifications.GetByUserAsync(currentUserId, isRead, cancellationToken);
        return _mapper.Map<List<NotificationResponse>>(notifications);
    }

    public Task<int> GetUnreadCountAsync(Guid currentUserId, CancellationToken cancellationToken = default)
        => _unitOfWork.Notifications.CountUnreadAsync(currentUserId, cancellationToken);

    public async Task MarkAsReadAsync(Guid notificationId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId, cancellationToken)
            ?? throw new NotFoundException("Обавјештење није пронађено.");

        if (notification.UserId != currentUserId)
            throw new ForbiddenException("Не можете мијењати туђа обавјештења.");

        if (notification.IsRead)
            return;

        notification.MarkAsRead();
        _unitOfWork.Notifications.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var unread = await _unitOfWork.Notifications.GetByUserAsync(currentUserId, isRead: false, cancellationToken);
        if (unread.Count == 0)
            return;

        foreach (var notification in unread)
        {
            notification.MarkAsRead();
            _unitOfWork.Notifications.Update(notification);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
