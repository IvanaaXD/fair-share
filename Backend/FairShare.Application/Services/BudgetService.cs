using System.Text.RegularExpressions;
using FairShare.Application.DTOs.Budgets;
using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;
using FairShare.Application.Interfaces;
using TimeSheet.Application.Common.Exceptions;

namespace FairShare.Application.Services;

public class BudgetService : IBudgetService
{
    private static readonly Regex MonthFormat = new(@"^\d{4}-(0[1-9]|1[0-2])$", RegexOptions.Compiled);

    private readonly IUnitOfWork _unitOfWork;

    public BudgetService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BudgetResponse> CreateAsync(
        CreateBudgetRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.MonthlyLimit <= 0)
            throw new ConflictException("Мјесечни лимит мора бити већи од нуле.");

        ValidateMonthFormat(request.Month);

        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Категорија није пронађена.");

        var existing = await _unitOfWork.Budgets.GetByUserCategoryAndMonthAsync(
            currentUserId, request.CategoryId, request.Month, cancellationToken);
        if (existing is not null)
            throw new ConflictException("Буџет за ову категорију и мјесец већ постоји.");

        var budget = new Budget
        {
            UserId = currentUserId,
            CategoryId = request.CategoryId,
            Category = category,
            MonthlyLimit = request.MonthlyLimit,
            Month = request.Month
        };

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
        if (request.MonthlyLimit <= 0)
            throw new ConflictException("Мјесечни лимит мора бити већи од нуле.");

        var budget = await _unitOfWork.Budgets.GetByIdAsync(budgetId, cancellationToken)
            ?? throw new NotFoundException("Буџет није пронађен.");

        if (budget.UserId != currentUserId)
            throw new ForbiddenException("Не можете уређивати туђи буџет.");

        budget.MonthlyLimit = request.MonthlyLimit;

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
        ValidateMonthFormat(month);

        var budgets = await _unitOfWork.Budgets.GetByUserAndMonthAsync(currentUserId, month, cancellationToken);

        var result = new List<BudgetResponse>();
        foreach (var budget in budgets)
            result.Add(await MapToResponseAsync(budget, cancellationToken));

        return result;
    }

    // ---------- помоћне методе ----------

    private async Task<BudgetResponse> MapToResponseAsync(Budget budget, CancellationToken cancellationToken)
    {
        var (start, end) = GetMonthRange(budget.Month);

        var spending = await _unitOfWork.Expenses.GetTotalByUserAndCategoryAsync(
            budget.UserId, budget.CategoryId, start, end, cancellationToken);

        var category = budget.Category
            ?? await _unitOfWork.Categories.GetByIdAsync(budget.CategoryId, cancellationToken);

        return new BudgetResponse
        {
            Id = budget.Id,
            CategoryId = budget.CategoryId,
            CategoryName = category?.Name ?? string.Empty,
            MonthlyLimit = budget.MonthlyLimit,
            Month = budget.Month,
            CurrentSpending = spending,
            PercentageUsed = budget.MonthlyLimit > 0
                ? (double)(spending / budget.MonthlyLimit) * 100
                : 0
        };
    }

    private static void ValidateMonthFormat(string month)
    {
        if (string.IsNullOrWhiteSpace(month) || !MonthFormat.IsMatch(month))
            throw new ConflictException("Формат мјесеца мора бити ГГГГ-ММ (нпр. 2026-09).");
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
