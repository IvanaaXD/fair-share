namespace FairShare.Application.DTOs.Groups;

/// <summary>Позивница по email адреси - корисник мора већ бити регистрован на систему.</summary>
public class AddGroupMemberRequest
{
    public string Email { get; set; } = string.Empty;
}
