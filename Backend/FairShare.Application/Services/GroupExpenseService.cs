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

    public GroupExpenseService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupExpenseResponse> CreateAsync(
        Guid groupId,
        CreateGroupExpenseRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _unitOfWork.GroupMembers.IsMemberAsync(groupId, currentUserId, cancellationToken))
            throw new ForbiddenException("Само члан групе може додати групни трошак.");

        if (request.Participants.Select(p => p.UserId).Distinct().Count() != request.Participants.Count)
            throw new ConflictException("Листа учесника садржи дупликате.");

        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Категорија није пронађена.");

        var payer = await _unitOfWork.Users.GetByIdAsync(request.PaidByUserId, cancellationToken)
            ?? throw new NotFoundException("Корисник који је платио трошак не постоји.");

        if (!await _unitOfWork.GroupMembers.IsMemberAsync(groupId, request.PaidByUserId, cancellationToken))
            throw new NotFoundException("Корисник који је платио трошак није члан ове групе.");

        // Учитавамо учеснике и провјеравамо да су сви чланови групе
        var participantUsers = new Dictionary<Guid, User>();
        foreach (var participant in request.Participants)
        {
            if (!await _unitOfWork.GroupMembers.IsMemberAsync(groupId, participant.UserId, cancellationToken))
                throw new NotFoundException($"Корисник '{participant.UserId}' није члан ове групе.");

            var user = await _unitOfWork.Users.GetByIdAsync(participant.UserId, cancellationToken)
                ?? throw new NotFoundException($"Корисник '{participant.UserId}' не постоји.");

            participantUsers[participant.UserId] = user;
        }

        // Рачунање подјеле - сва валидација (суме, проценти, минимум учесника) живи у домени
        var splitInputs = request.Participants
            .Select(p => new SplitParticipantInput(p.UserId, p.Amount, p.Percentage))
            .ToList();

        var splits = ExpenseSplitCalculator.Calculate(request.Amount, request.SplitType, splitInputs);
        foreach (var split in splits)
            split.User = participantUsers[split.UserId];

        var groupExpense = new GroupExpense
        {
            GroupId = groupId,
            CategoryId = request.CategoryId,
            Category = category,
            PaidByUserId = request.PaidByUserId,
            PaidByUser = payer,
            Amount = request.Amount,
            Description = request.Description,
            Date = request.Date,
            SplitType = request.SplitType
        };

        foreach (var split in splits)
            split.GroupExpenseId = groupExpense.Id;

        groupExpense.Splits = splits;

        await _unitOfWork.GroupExpenses.AddAsync(groupExpense, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(groupExpense);
    }

    public async Task<IReadOnlyList<GroupExpenseResponse>> GetByGroupAsync(
        Guid groupId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var expenses = await _unitOfWork.GroupExpenses.GetByGroupAsync(groupId, page, pageSize, cancellationToken);
        return expenses.Select(MapToResponse).ToList();
    }

    private static GroupExpenseResponse MapToResponse(GroupExpense expense) => new()
    {
        Id = expense.Id,
        GroupId = expense.GroupId,
        CategoryId = expense.CategoryId,
        CategoryName = expense.Category.Name,
        PaidByUserId = expense.PaidByUserId,
        PaidByName = $"{expense.PaidByUser.FirstName} {expense.PaidByUser.LastName}",
        Amount = expense.Amount,
        Description = expense.Description,
        Date = expense.Date,
        SplitType = expense.SplitType,
        Splits = expense.Splits.Select(s => new ExpenseSplitResponse
        {
            UserId = s.UserId,
            FirstName = s.User.FirstName,
            LastName = s.User.LastName,
            Amount = s.Amount,
            Percentage = s.Percentage
        }).ToList()
    };
}
