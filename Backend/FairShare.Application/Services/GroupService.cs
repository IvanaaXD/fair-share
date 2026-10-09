using AutoMapper;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.DTOs.Groups;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Services;

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

    public async Task<GroupResponse> GetGroupAsync(
        Guid groupId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var group = await LoadGroupAsync(groupId, cancellationToken);
        GetMembership(group, currentUserId);

        return _mapper.Map<GroupResponse>(group);
    }

    public async Task<GroupResponse> UpdateGroupAsync(
        Guid groupId,
        UpdateGroupRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var group = await LoadGroupAsync(groupId, cancellationToken);
        EnsureOwner(GetMembership(group, currentUserId), "Само власник групе може мијењати податке о групи.");

        _mapper.Map(request, group);

        _unitOfWork.Groups.Update(group);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<GroupResponse>(group);
    }

    public async Task DeleteGroupAsync(Guid groupId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var group = await LoadGroupAsync(groupId, cancellationToken);
        EnsureOwner(GetMembership(group, currentUserId), "Само власник групе може обрисати групу.");

        var balances = await _unitOfWork.GetGroupNetBalancesAsync(groupId, cancellationToken);
        if (!balances.Values.All(GroupBalanceCalculator.IsSettled))
            throw new ConflictException("Група се не може обрисати док постоје неизмирена дуговања.");

        // Settlements reference the group with ON DELETE RESTRICT, so they are removed explicitly.
        // Members, expenses, splits and comments are removed by cascade delete.
        var settlements = await _unitOfWork.SettlementTransactions.GetForUpdateByGroupAsync(
            groupId, null, cancellationToken);
        foreach (var settlement in settlements)
            _unitOfWork.SettlementTransactions.Remove(settlement);

        _unitOfWork.Groups.Remove(group);

        // One SaveChanges = one transaction: either the whole group disappears or nothing does.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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

    public async Task RemoveMemberAsync(
        Guid groupId,
        Guid userId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var group = await LoadGroupAsync(groupId, cancellationToken);
        EnsureOwner(GetMembership(group, currentUserId), "Само власник групе може уклањати чланове.");

        if (userId == currentUserId)
            throw new ConflictException("Не можете уклонити себе. Ако желите изаћи из групе, користите напуштање групе.");

        var target = group.Members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new NotFoundException("Корисник није члан ове групе.");

        await EnsureBalanceSettledAsync(
            groupId, userId,
            "Члан се не може уклонити док има неизмирена дуговања или потраживања у групи.",
            cancellationToken);

        // The member's balance is zero, so any suggestion that still involves them is out of date.
        await _unitOfWork.RemoveProposedSettlementsAsync(groupId, userId, cancellationToken);
        _unitOfWork.GroupMembers.Remove(target);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyAsync(
            userId,
            NotificationType.GroupMembershipChanged,
            "Уклоњени сте из групе",
            $"Власник групе '{group.Name}' вас је уклонио из групе.",
            cancellationToken);
    }

    public async Task LeaveGroupAsync(Guid groupId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var group = await LoadGroupAsync(groupId, cancellationToken);
        var membership = GetMembership(group, currentUserId);

        var otherMembers = group.Members
            .Where(m => m.UserId != currentUserId)
            .OrderBy(m => m.JoinedAt)
            .ToList();

        if (otherMembers.Count == 0)
            throw new ConflictException("Ви сте једини члан групе. Умјесто напуштања, обришите групу.");

        await EnsureBalanceSettledAsync(
            groupId, currentUserId,
            "Не можете напустити групу док имате неизмирена дуговања или потраживања.",
            cancellationToken);

        // A group must always have an owner: when the owner leaves, the member who has been
        // in the group the longest takes over.
        GroupMember? newOwner = null;
        if (membership.Role == GroupRole.Owner)
        {
            newOwner = otherMembers[0];
            newOwner.Role = GroupRole.Owner;
        }

        await _unitOfWork.RemoveProposedSettlementsAsync(groupId, currentUserId, cancellationToken);
        _unitOfWork.GroupMembers.Remove(membership);

        // Leaving and the ownership transfer are saved together, in one transaction.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (newOwner is not null)
        {
            await _notificationService.NotifyAsync(
                newOwner.UserId,
                NotificationType.GroupMembershipChanged,
                "Постали сте власник групе",
                $"Претходни власник је напустио групу '{group.Name}', па сте ви сада њен власник.",
                cancellationToken);
        }
    }

    public async Task<IReadOnlyList<GroupResponse>> GetMyGroupsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var groups = await _unitOfWork.Groups.GetByUserAsync(currentUserId, cancellationToken);
        return _mapper.Map<List<GroupResponse>>(groups);
    }

    // ---------- helpers ----------

    /// <summary>Loads the group together with its members and their users (tracked).</summary>
    private async Task<Group> LoadGroupAsync(Guid groupId, CancellationToken cancellationToken)
        => await _unitOfWork.Groups.GetWithMembersAsync(groupId, cancellationToken)
           ?? throw new NotFoundException("Група није пронађена.");

    /// <summary>Returns the current user's membership, or 403 if they are not in the group.</summary>
    private static GroupMember GetMembership(Group group, Guid userId)
        => group.Members.FirstOrDefault(m => m.UserId == userId)
           ?? throw new ForbiddenException("Нисте члан ове групе.");

    private static void EnsureOwner(GroupMember membership, string message)
    {
        if (membership.Role != GroupRole.Owner)
            throw new ForbiddenException(message);
    }

    private async Task EnsureBalanceSettledAsync(
        Guid groupId,
        Guid userId,
        string message,
        CancellationToken cancellationToken)
    {
        var balances = await _unitOfWork.GetGroupNetBalancesAsync(groupId, cancellationToken);

        if (!GroupBalanceCalculator.IsSettled(balances.GetValueOrDefault(userId, 0m)))
            throw new ConflictException(message);
    }
}
