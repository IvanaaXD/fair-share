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
    private readonly INotificationService _notificationService; // НОВО

    public GroupService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<GroupResponse> CreateGroupAsync(
        CreateGroupRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var creator = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException("Тренутно улогован корисник није пронађен.");

        var group = new Group
        {
            Name = request.Name,
            Description = request.Description,
            Currency = request.Currency,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Groups.AddAsync(group, cancellationToken);

        // Творац групе аутоматски постаје власник (GroupRole.Owner)
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
        // НОВО: учитавамо групу (провјера постојања + назив за обавјештење)
        var group = await _unitOfWork.Groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Група није пронађена.");

        var requesterIsMember = await _unitOfWork.GroupMembers.IsMemberAsync(groupId, currentUserId, cancellationToken);
        if (!requesterIsMember)
            throw new ForbiddenException("Само члан групе може додавати нове чланове.");

        var userToAdd = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new NotFoundException($"Корисник са email адресом '{request.Email}' не постоји.");

        var alreadyMember = await _unitOfWork.GroupMembers.IsMemberAsync(groupId, userToAdd.Id, cancellationToken);
        if (alreadyMember)
            throw new ConflictException("Корисник је већ члан ове групе.");

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

        // НОВО: обавјештење додатом кориснику
        await _notificationService.NotifyAsync(
            userToAdd.Id,
            NotificationType.GroupInvite,
            "Нова група",
            $"Додати сте у групу '{group.Name}'.",
            cancellationToken);

        return MapToMemberResponse(member);
    }

    public async Task<IReadOnlyList<GroupResponse>> GetMyGroupsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var groups = await _unitOfWork.Groups.GetByUserAsync(currentUserId, cancellationToken);
        return groups.Select(MapToGroupResponse).ToList();
    }

    // ---------- ручно мапирање (AutoMapper долази у каснијем кораку) ----------

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
