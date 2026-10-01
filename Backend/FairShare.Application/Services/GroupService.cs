using FairShare.Application.DTOs.Groups;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using TimeSheet.Application.Common.Exceptions;

namespace FairShare.Application.Services;

public class GroupService : IGroupService
{
    private readonly IUnitOfWork _unitOfWork;

    public GroupService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupResponse> CreateGroupAsync(
        CreateGroupRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var creator = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException("Currently logged in user not found.");

        var group = new Group
        {
            Name = request.Name,
            Description = request.Description,
            Currency = request.Currency,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Groups.AddAsync(group, cancellationToken);

        var owner = new GroupMember
        {
            GroupId = group.Id,
            UserId = currentUserId,
            User = creator,
            Role = GroupRole.Owner,
            JoinedAt = DateTime.UtcNow
        };

        await _unitOfWork.GroupMembers.AddAsync(owner, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        group.Members.Add(owner);
        return MapToGroupResponse(group);
    }

    public async Task<GroupMemberResponse> AddMemberAsync(
        Guid groupId,
        AddGroupMemberRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var requesterIsMember = await _unitOfWork.GroupMembers.IsMemberAsync(groupId, currentUserId, cancellationToken);
        if (!requesterIsMember)
            throw new ForbiddenException("Only group members can add new members.");

        var userToAdd = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new NotFoundException($"User with email address '{request.Email}' does not exist.");

        var alreadyMember = await _unitOfWork.GroupMembers.IsMemberAsync(groupId, userToAdd.Id, cancellationToken);
        if (alreadyMember)
            throw new ConflictException("User is already a member of this group.");

        var member = new GroupMember
        {
            GroupId = groupId,
            UserId = userToAdd.Id,
            User = userToAdd,
            Role = GroupRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        await _unitOfWork.GroupMembers.AddAsync(member, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToMemberResponse(member);
    }

    public async Task<IReadOnlyList<GroupResponse>> GetMyGroupsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var groups = await _unitOfWork.Groups.GetByUserAsync(currentUserId, cancellationToken);
        return groups.Select(MapToGroupResponse).ToList();
    }

    private static GroupResponse MapToGroupResponse(Group group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        Description = group.Description,
        Currency = group.Currency,
        CreatedAt = group.CreatedAt,
        Members = group.Members.Select(MapToMemberResponse).ToList()
    };

    private static GroupMemberResponse MapToMemberResponse(GroupMember member) => new()
    {
        UserId = member.UserId,
        FirstName = member.User.FirstName,
        LastName = member.User.LastName,
        Email = member.User.Email,
        Role = member.Role,
        JoinedAt = member.JoinedAt
    };
}