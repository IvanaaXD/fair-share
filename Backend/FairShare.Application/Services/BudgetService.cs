using System.Text.RegularExpressions;
using AutoMapper;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.DTOs.Budgets;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using TimeSheet.Application.Common.Exceptions;

namespace FairShare.Application.Services;

public class BudgetService : IBudgetService
{
    private static readonly Regex MonthFormat = new(@"^\d{4}-(0[1-9]|1[0-2])$", RegexOptions.Compiled);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public BudgetService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BudgetResponse> CreateAsync(
        CreateBudgetRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // Limit and month format are checked by CreateBudgetRequestValidator.
        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Категорија није пронађена.");

        var existing = await _unitOfWork.Budgets.GetByUserCategoryAndMonthAsync(
            currentUserId, request.CategoryId, request.Month, cancellationToken);
        if (existing is not null)
            throw new ConflictException("Буџет за ову категорију и мјесец већ постоји.");

        var budget = _mapper.Map<Budget>(request);
        budget.UserId = currentUserId;
        budget.Category = category;

        await _unitOfWork.Budgets.AddAsync(budget, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToResponseAsync(budget, cancellationToken);
    }

    public async Task<BudgetResponse> UpdateAsync(
        Guid budgetId,
        UpdateBudgetRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var budget = await _unitOfWork.Budgets.GetByIdAsync(budgetId, cancellationToken)
            ?? throw new NotFoundException("Буџет није пронађен.");

        if (budget.UserId != currentUserId)
            throw new ForbiddenException("Не можете уређивати туђи буџет.");

        // Only the limit can change; category and month are fixed for an existing budget.
        _mapper.Map(request, budget);

        _unitOfWork.Budgets.Update(budget);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToResponseAsync(budget, cancellationToken);
    }

    public async Task DeleteAsync(Guid budgetId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var budget = await _unitOfWork.Budgets.GetByIdAsync(budgetId, cancellationToken)
            ?? throw new NotFoundException("Буџет није пронађен.");

        if (budget.UserId != currentUserId)
            throw new ForbiddenException("Не можете обрисати туђи буџет.");

        _unitOfWork.Budgets.Remove(budget);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BudgetResponse>> GetMyBudgetsAsync(
        Guid currentUserId,
        string month,
        CancellationToken cancellationToken = default)
    {
        // "month" comes from the query string, not from a DTO, so it is validated here.
        if (string.IsNullOrWhiteSpace(month) || !MonthFormat.IsMatch(month))
            throw new BadRequestException("Формат мјесеца мора бити ГГГГ-ММ (нпр. 2026-09).");

        var budgets = await _unitOfWork.Budgets.GetByUserAndMonthAsync(currentUserId, month, cancellationToken);

        var result = new List<BudgetResponse>();
        foreach (var budget in budgets)
            result.Add(await MapToResponseAsync(budget, cancellationToken));

        return result;
    }

    // ---------- helpers ----------

    /// <summary>Maps the budget and adds the spending calculated from expenses in that month.</summary>
    private async Task<BudgetResponse> MapToResponseAsync(Budget budget, CancellationToken cancellationToken)
    {
        // Budgets loaded with AsNoTracking have no Category; load it so CategoryName can be mapped.
        if (budget.Category is null)
            budget.Category = (await _unitOfWork.Categories.GetByIdAsync(budget.CategoryId, cancellationToken))!;

        var (start, end) = GetMonthRange(budget.Month);
        var spending = await _unitOfWork.Expenses.GetTotalByUserAndCategoryAsync(
            budget.UserId, budget.CategoryId, start, end, cancellationToken);

        var response = _mapper.Map<BudgetResponse>(budget);
        response.CurrentSpending = spending;
        response.PercentageUsed = budget.MonthlyLimit > 0
            ? (double)(spending / budget.MonthlyLimit) * 100
            : 0;

        return response;
    }

    private static (DateTime Start, DateTime End) GetMonthRange(string month)
    {
        var year = int.Parse(month[..4]);
        var monthNumber = int.Parse(month[5..7]);
        var start = new DateTime(year, monthNumber, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddTicks(-1);
        return (start, end);
    }
}
