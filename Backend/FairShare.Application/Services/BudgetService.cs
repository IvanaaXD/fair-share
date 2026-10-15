using System.Text.RegularExpressions;
using AutoMapper;
using FairShare.Application.Abstractions;
using FairShare.Application.Common.Currency;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.DTOs.Budgets;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Interfaces;

namespace FairShare.Application.Services;

public class BudgetService : IBudgetService
{
    private static readonly Regex MonthFormat = new(@"^\d{4}-(0[1-9]|1[0-2])$", RegexOptions.Compiled);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IExchangeRateProvider _exchangeRates;
    private readonly IMapper _mapper;
    private readonly TimeProvider _time;

    public BudgetService(IUnitOfWork unitOfWork, IExchangeRateProvider exchangeRates, IMapper mapper, TimeProvider time)
    {
        _unitOfWork = unitOfWork;
        _exchangeRates = exchangeRates;
        _mapper = mapper;
        _time = time;
    }

    public async Task<BudgetResponse> CreateAsync(
            CreateBudgetRequest request,
            Guid currentUserId,
            CancellationToken cancellationToken = default)
    {
        // Limit and month format are checked by CreateBudgetRequestValidator.
        var now = _time.GetUtcNow().UtcDateTime;
        if (Budget.IsMonthClosed(request.Month, now))
            throw new BadRequestException(
                "Буџет се не може поставити за мјесец који је прошао.", ErrorCodes.BudgetMonthClosed);
        if (Budget.IsMonthTooFarAhead(request.Month, now))
            throw new BadRequestException(
                $"Буџет се може планирати највише {Budget.MaxMonthsAhead} мјесеци унапријед.", ErrorCodes.BudgetMonthTooFar);

        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Категорија није пронађена.", ErrorCodes.CategoryNotFound);

        var existing = await _unitOfWork.Budgets.GetByUserCategoryAndMonthAsync(
            currentUserId, request.CategoryId, request.Month, cancellationToken);
        if (existing is not null)
            throw new ConflictException("Буџет за ову категорију и мјесец већ постоји.", ErrorCodes.BudgetExists);

        var budget = _mapper.Map<Budget>(request);
        budget.UserId = currentUserId;
        budget.Category = category;

        await _unitOfWork.Budgets.AddAsync(budget, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToResponseAsync(budget, await SpendingContextAsync(currentUserId, cancellationToken), cancellationToken);
    }

    public async Task<BudgetResponse> UpdateAsync(
        Guid budgetId,
        UpdateBudgetRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var budget = await GetOwnOpenBudgetAsync(budgetId, currentUserId, cancellationToken);

        // Only the limit can change; category and month are fixed for an existing budget.
        _mapper.Map(request, budget);

        _unitOfWork.Budgets.Update(budget);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToResponseAsync(budget, await SpendingContextAsync(currentUserId, cancellationToken), cancellationToken);
    }

    public async Task DeleteAsync(Guid budgetId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var budget = await GetOwnOpenBudgetAsync(budgetId, currentUserId, cancellationToken);

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
            throw new BadRequestException("Мјесец мора бити у облику ГГГГ-ММ (нпр. 2026-09).", ErrorCodes.InvalidMonth);

        var budgets = await _unitOfWork.Budgets.GetByUserAndMonthAsync(currentUserId, month, cancellationToken);
        var context = await SpendingContextAsync(currentUserId, cancellationToken);

        var result = new List<BudgetResponse>();
        foreach (var budget in budgets)
            result.Add(await MapToResponseAsync(budget, context, cancellationToken));

        return result;
    }

    // ---------- helpers ----------

    /// <summary>
    /// The user's own budget of the current or a future month. Someone else's budget is reported
    /// exactly like a missing one; a budget of a past month is history and stays as it was.
    /// </summary>
    private async Task<Budget> GetOwnOpenBudgetAsync(Guid budgetId, Guid currentUserId, CancellationToken cancellationToken)
    {
        var budget = await _unitOfWork.Budgets.GetByIdAsync(budgetId, cancellationToken);
        if (budget is null || budget.UserId != currentUserId)
            throw new NotFoundException("Буџет није пронађен.", ErrorCodes.BudgetNotFound);

        if (budget.IsClosed(_time.GetUtcNow().UtcDateTime))
            throw new ConflictException(
                "Буџет за мјесец који је прошао не може се мијењати ни брисати.", ErrorCodes.BudgetMonthClosed);

        return budget;
    }

    /// <summary>
    /// A budget limit is in the user's default currency. Expenses in other currencies are
    /// converted with today's rates before they count towards it.
    /// </summary>
    private async Task<(string Currency, ExchangeRates Rates)> SpendingContextAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        var rates = await _exchangeRates.GetRatesAsync(cancellationToken);
        return (user?.DefaultCurrency ?? "BAM", rates);
    }

    /// <summary>Maps the budget and adds the spending calculated from expenses in that month.</summary>
    private async Task<BudgetResponse> MapToResponseAsync(
        Budget budget,
        (string Currency, ExchangeRates Rates) context,
        CancellationToken cancellationToken)
    {
        // Budgets loaded with AsNoTracking have no Category; load it so CategoryName can be mapped.
        if (budget.Category is null)
            budget.Category = (await _unitOfWork.Categories.GetByIdAsync(budget.CategoryId, cancellationToken))!;

        var (start, end) = GetMonthRange(budget.Month);
        var totals = await _unitOfWork.Expenses.GetTotalsByCurrencyAsync(
            budget.UserId, budget.CategoryId, start, end, cancellationToken);
        var spending = context.Rates.SumIn(context.Currency, totals);

        var response = _mapper.Map<BudgetResponse>(budget);
        response.CurrentSpending = spending;
        response.PercentageUsed = budget.MonthlyLimit > 0
            ? (double)(spending / budget.MonthlyLimit) * 100
            : 0;

        return response;
    }

    private static (DateTime Start, DateTime End) GetMonthRange(string month)
    {
        var start = Budget.MonthStart(month);
        return (start, start.AddMonths(1).AddTicks(-1));
    }
}
