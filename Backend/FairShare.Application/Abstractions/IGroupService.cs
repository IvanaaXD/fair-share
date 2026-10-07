using FairShare.Application.DTOs.Groups;

namespace FairShare.Application.Interfaces;

public interface IGroupService
{
    /// <summary>Creates a group; the creator automatically becomes its owner.</summary>
    Task<GroupResponse> CreateGroupAsync(
        CreateGroupRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Group details with members. Members only.</summary>
    Task<GroupResponse> GetGroupAsync(
        Guid groupId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Changes the group name and description. Owner only.</summary>
    Task<GroupResponse> UpdateGroupAsync(
        Guid groupId,
        UpdateGroupRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the group with its whole history. Owner only, and only when nobody owes anything.</summary>
    Task DeleteGroupAsync(Guid groupId, Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>Adds an existing user (by e-mail) to the group.</summary>
    Task<GroupMemberResponse> AddMemberAsync(
        Guid groupId,
        AddGroupMemberRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a member. Owner only, and only when that member's balance is settled.</summary>
    Task RemoveMemberAsync(
        Guid groupId,
        Guid userId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The current user leaves the group, allowed only when their balance is settled.
    /// If the owner leaves, ownership passes to the member who has been in the group the longest.
    /// </summary>
    Task LeaveGroupAsync(Guid groupId, Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>All groups the current user is a member of.</summary>
    Task<IReadOnlyList<GroupResponse>> GetMyGroupsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}
