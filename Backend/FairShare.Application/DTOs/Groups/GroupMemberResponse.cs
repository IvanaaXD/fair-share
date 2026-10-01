using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.DTOs.Groups;

public class GroupMemberResponse
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public GroupRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
