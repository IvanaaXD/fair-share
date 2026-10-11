using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public void MarkAsRead() => IsRead = true;
}
