using FairShare.Application.DTOs.Groups;

namespace FairShare.Application.Interfaces;

public interface IGroupService
{
    /// <summary>Креира нову групу; творац групе аутоматски постаје члан са улогом Owner.</summary>
    Task<GroupResponse> CreateGroupAsync(
        CreateGroupRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Додаје постојећег корисника (по email адреси) у групу.</summary>
    Task<GroupMemberResponse> AddMemberAsync(
        Guid groupId,
        AddGroupMemberRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Враћа све групе у којима је тренутни корисник члан.</summary>
    Task<IReadOnlyList<GroupResponse>> GetMyGroupsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}
