namespace FairShare.Application.DTOs.Groups;

/// <summary>
/// Only the name and description can be changed. The currency is fixed once the group is
/// created, because existing expenses and settlements are recorded in it.
/// </summary>
public class UpdateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
