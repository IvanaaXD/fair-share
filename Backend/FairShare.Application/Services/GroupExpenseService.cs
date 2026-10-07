using AutoMapper;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.DTOs.GroupExpenses;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Services;
using TimeSheet.Application.Common.Exceptions;

namespace FairShare.Application.Services;

public class GroupExpenseService : IGroupExpenseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GroupExpenseService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<GroupExpenseResponse> CreateAsync(
        Guid groupId,
        CreateGroupExpenseRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // Request shape (amount, participants, sums, duplicates) is checked by
        // CreateGroupExpenseRequestValidator. Here we check what only the database knows.
        if (!await _unitOfWork.GroupMembers.IsMemberAsync(groupId, currentUserId, cancellationToken))
            throw new ForbiddenException("Само члан групе може додати групни трошак.");

        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Категорија није пронађена.");

        var payer = await _unitOfWork.Users.GetByIdAsync(request.PaidByUserId, cancellationToken)
            ?? throw new NotFoundException("Корисник који је платио трошак не постоји.");

        if (!await _unitOfWork.GroupMembers.IsMemberAsync(groupId, request.PaidByUserId, cancellationToken))
            throw new NotFoundException("Корисник који је платио трошак није члан ове групе.");

        // Every participant must be a member of the group.
        var participantUsers = new Dictionary<Guid, User>();
        foreach (var participant in request.Participants)
        {
            if (!await _unitOfWork.GroupMembers.IsMemberAsync(groupId, participant.UserId, cancellationToken))
                throw new NotFoundException($"Корисник '{participant.UserId}' није члан ове групе.");

            var user = await _unitOfWork.Users.GetByIdAsync(participant.UserId, cancellationToken)
                ?? throw new NotFoundException($"Корисник '{participant.UserId}' не постоји.");

            participantUsers[participant.UserId] = user;
        }

        // Split calculation (including rounding remainders) is pure domain logic.
        var splitInputs = request.Participants
            .Select(p => new SplitParticipantInput(p.UserId, p.Amount, p.Percentage))
            .ToList();

        var splits = ExpenseSplitCalculator.Calculate(request.Amount, request.SplitType, splitInputs);

        var groupExpense = _mapper.Map<GroupExpense>(request);
        groupExpense.GroupId = groupId;
        groupExpense.Category = category;
        groupExpense.PaidByUser = payer;

        foreach (var split in splits)
        {
            split.GroupExpenseId = groupExpense.Id;
            split.User = participantUsers[split.UserId];
        }

        groupExpense.Splits = splits;

        // The expense and all its splits are saved in one SaveChanges call, i.e. one transaction.
        await _unitOfWork.GroupExpenses.AddAsync(groupExpense, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<GroupExpenseResponse>(groupExpense);
    }

    public async Task<IReadOnlyList<GroupExpenseResponse>> GetByGroupAsync(
        Guid groupId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var expenses = await _unitOfWork.GroupExpenses.GetByGroupAsync(groupId, page, pageSize, cancellationToken);
        return _mapper.Map<List<GroupExpenseResponse>>(expenses);
    }
}
