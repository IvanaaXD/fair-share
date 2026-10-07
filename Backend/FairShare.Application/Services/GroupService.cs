using AutoMapper;
using FairShare.Application.Common.Exceptions;
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
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;

    public GroupService(IUnitOfWork unitOfWork, INotificationService notificationService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _mapper = mapper;
    }

    public async Task<GroupResponse> CreateGroupAsync(
        CreateGroupRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var creator = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException("Тренутно улогован корисник није пронађен.");

        var group = _mapper.Map<Group>(request);
        group.CreatedAt = DateTime.UtcNow;

        await _unitOfWork.Groups.AddAsync(group, cancellationToken);

        // The creator automatically becomes the group owner.
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

        // EF relationship fix-up usually adds the owner to group.Members already;
        // the check prevents the owner from appearing twice in the response.
        if (!group.Members.Contains(owner))
            group.Members.Add(owner);

        return _mapper.Map<GroupResponse>(group);
    }

    public async Task<GroupMemberResponse> AddMemberAsync(
        Guid groupId,
        AddGroupMemberRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var group = await _unitOfWork.Groups.GetByIdAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Група није пронађена.");

        if (!await _unitOfWork.GroupMembers.IsMemberAsync(groupId, currentUserId, cancellationToken))
            throw new ForbiddenException("Само члан групе може додавати нове чланове.");

        var userToAdd = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new NotFoundException($"Корисник са email адресом '{request.Email}' не постоји.");

        if (await _unitOfWork.GroupMembers.IsMemberAsync(groupId, userToAdd.Id, cancellationToken))
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

        await _notificationService.NotifyAsync(
            userToAdd.Id,
            NotificationType.GroupInvite,
            "Нова група",
            $"Додати сте у групу '{group.Name}'.",
            cancellationToken);

        return _mapper.Map<GroupMemberResponse>(member);
    }

    public async Task<IReadOnlyList<GroupResponse>> GetMyGroupsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var groups = await _unitOfWork.Groups.GetByUserAsync(currentUserId, cancellationToken);
        return _mapper.Map<List<GroupResponse>>(groups);
    }
}
