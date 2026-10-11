namespace FairShare.Application.DTOs.Groups;

/// <summary>Invitation by email address - the user must already be registered in the system.</summary>
public class AddGroupMemberRequest
{
    public string Email { get; set; } = string.Empty;
}
