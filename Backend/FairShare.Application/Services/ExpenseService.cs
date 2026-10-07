using AutoMapper;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.DTOs.Expenses;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using TimeSheet.Application.Common.Exceptions;

namespace FairShare.Application.Services;

public class ExpenseService : IExpenseService
{
    // Budget thresholds, checked from highest to lowest so only one notification is sent
    // (for the highest threshold that was just crossed).
    private static readonly (decimal Fraction, string Label)[] BudgetThresholds =
    {
        (1.0m, "100%"),
        (0.8m, "80%")
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;

    public ExpenseService(IUnitOfWork unitOfWork, INotificationService notificationService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _mapper = mapper;
    }

    public async Task<ExpenseResponse> CreateAsync(
        CreateExpenseRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        // Field formats (amount > 0, currency, date...) are checked by CreateExpenseRequestValidator.
        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Категорија није пронађена.");

        var expense = _mapper.Map<Expense>(request);
        expense.UserId = currentUserId;
        expense.Category = category;

        // Spending BEFORE this expense - needed to detect whether this expense is the one
        // that crosses a budget threshold (and not every expense after it).
        var monthStart = new DateTime(expense.Date.Year, expense.Date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
        var previousTotal = await _unitOfWork.Expenses.GetTotalByUserAndCategoryAsync(
            currentUserId, expense.CategoryId, monthStart, monthEnd, cancellationToken);

        await _unitOfWork.Expenses.AddAsync(expense, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var budgetMessage = await GetCrossedBudgetThresholdMessageAsync(
            currentUserId, expense.CategoryId, category.Name, expense.Currency,
            expense.Date.ToString("yyyy-MM"), previousTotal, previousTotal + expense.Amount, cancellationToken);

        if (budgetMessage is not null)
        {
            await _notificationService.NotifyAsync(
                currentUserId,
                NotificationType.BudgetExceeded,
                "Прекорачење буџета",
                budgetMessage,
                cancellationToken);
        }

        return _mapper.Map<ExpenseResponse>(expense);
    }

    public async Task<ExpenseResponse> UpdateAsync(
        Guid expenseId,
        UpdateExpenseRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var expense = await _unitOfWork.Expenses.GetByIdAsync(expenseId, cancellationToken)
            ?? throw new NotFoundException("Трошак није пронађен.");

        if (expense.UserId != currentUserId)
            throw new ForbiddenException("Не можете уређивати туђи трошак.");

        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Категорија није пронађена.");

        // Overwrites only the fields present in the request; Id and UserId stay untouched.
        _mapper.Map(request, expense);
        expense.Category = category;

        _unitOfWork.Expenses.Update(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Note: budget thresholds are checked only when an expense is created.

        return _mapper.Map<ExpenseResponse>(expense);
    }

    public async Task DeleteAsync(Guid expenseId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var expense = await _unitOfWork.Expenses.GetByIdAsync(expenseId, cancellationToken)
            ?? throw new NotFoundException("Трошак није пронађен.");

        if (expense.UserId != currentUserId)
            throw new ForbiddenException("Не можете обрисати туђи трошак.");

        _unitOfWork.Expenses.Remove(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExpenseResponse>> GetMyExpensesAsync(
        Guid currentUserId,
        DateTime? from = null,
        DateTime? to = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var expenses = await _unitOfWork.Expenses.GetByUserAsync(currentUserId, from, to, categoryId, cancellationToken);
        return _mapper.Map<List<ExpenseResponse>>(expenses);
    }

    // ---------- budget threshold check ----------

    /// <summary>Returns the notification text if this expense just crossed a threshold, otherwise null.</summary>
    private async Task<string?> GetCrossedBudgetThresholdMessageAsync(
        Guid userId,
        Guid categoryId,
        string categoryName,
        string currency,
        string month,
        decimal previousTotal,
        decimal newTotal,
        CancellationToken cancellationToken)
    {
        var budget = await _unitOfWork.Budgets.GetByUserCategoryAndMonthAsync(userId, categoryId, month, cancellationToken);
        if (budget is null || budget.MonthlyLimit <= 0)
            return null;

        foreach (var (fraction, label) in BudgetThresholds)
        {
            var limitAtThreshold = budget.MonthlyLimit * fraction;

            // Notify only at the moment a threshold is crossed, not for every expense above it.
            if (previousTotal < limitAtThreshold && newTotal >= limitAtThreshold)
            {
                return $"Потрошња у категорији '{categoryName}' достигла је {label} мјесечног " +
                       $"лимита од {budget.MonthlyLimit} {currency} за {month}.";
            }
        }

        return null;
    }
}
