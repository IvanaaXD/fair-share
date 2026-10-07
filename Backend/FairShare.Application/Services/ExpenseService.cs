using FairShare.Application.DTOs.Expenses;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using TimeSheet.Application.Common.Exceptions;

namespace FairShare.Application.Services;

public class ExpenseService : IExpenseService
{
    // Прагови прекорачења буџета; провјеравају се од највишег ка најнижем, да се
    // пошаље само једно обавјештење (за највиши праг који је управо пређен).
    private static readonly (decimal Fraction, string Label)[] BudgetThresholds =
    {
        (1.0m, "100%"),
        (0.8m, "80%")
    };

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService; // НОВО

    public ExpenseService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<ExpenseResponse> CreateAsync(
        CreateExpenseRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
            throw new ConflictException("Износ трошка мора бити већи од нуле.");

        var category = await _unitOfWork.Categories.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw new NotFoundException("Категорија није пронађена.");

        // Потрошња ПРИЈЕ додавања овог трошка - потребно да бисмо открили да ли се
        // овим трошком тек сада "прелази" праг.
        var monthStart = new DateTime(request.Date.Year, request.Date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
        var previousTotal = await _unitOfWork.Expenses.GetTotalByUserAndCategoryAsync(
            currentUserId, request.CategoryId, monthStart, monthEnd, cancellationToken);

        var expense = new Expense
        {
            UserId = currentUserId,
            CategoryId = request.CategoryId,
            Category = category,
            Amount = request.Amount,
            Currency = request.Currency,
            Date = request.Date,
            Description = request.Description,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ReceiptImageUrl = request.ReceiptImageUrl,
            IsRecurring = request.IsRecurring,
            RecurrenceInterval = request.RecurrenceInterval
        };

        await _unitOfWork.Expenses.AddAsync(expense, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ИЗМЈЕНА: обавјештење сада иде преко NotificationService-а (чува се у бази И шаље e-mail)
        var budgetMessage = await GetCrossedBudgetThresholdMessageAsync(
            currentUserId, request.CategoryId, category.Name, request.Currency,
            request.Date.ToString("yyyy-MM"), previousTotal, previousTotal + request.Amount, cancellationToken);

        if (budgetMessage is not null)
        {
            await _notificationService.NotifyAsync(
                currentUserId,
                NotificationType.BudgetExceeded,
                "Прекорачење буџета",
                budgetMessage,
                cancellationToken);
        }

        return MapToResponse(expense);
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

        expense.CategoryId = request.CategoryId;
        expense.Category = category;
        expense.Amount = request.Amount;
        expense.Currency = request.Currency;
        expense.Date = request.Date;
        expense.Description = request.Description;
        expense.Latitude = request.Latitude;
        expense.Longitude = request.Longitude;
        expense.ReceiptImageUrl = request.ReceiptImageUrl;
        expense.IsRecurring = request.IsRecurring;
        expense.RecurrenceInterval = request.RecurrenceInterval;

        _unitOfWork.Expenses.Update(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // НАПОМЕНА: праг буџета се провјерава само при креирању трошка.

        return MapToResponse(expense);
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
        return expenses.Select(MapToResponse).ToList();
    }

    // ---------- провјера прага буџета ----------

    /// <summary>Враћа текст обавјештења ако је овим трошком управо пређен неки праг, иначе null.</summary>
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

            // Обавјештење само у тренутку преласка прага, не за сваки сљедећи трошак изнад њега.
            if (previousTotal < limitAtThreshold && newTotal >= limitAtThreshold)
            {
                return $"Потрошња у категорији '{categoryName}' достигла је {label} мјесечног " +
                       $"лимита од {budget.MonthlyLimit} {currency} за {month}.";
            }
        }

        return null;
    }

    private static ExpenseResponse MapToResponse(Expense expense) => new()
    {
        Id = expense.Id,
        CategoryId = expense.CategoryId,
        CategoryName = expense.Category?.Name ?? string.Empty,
        Amount = expense.Amount,
        Currency = expense.Currency,
        Date = expense.Date,
        Description = expense.Description,
        Latitude = expense.Latitude,
        Longitude = expense.Longitude,
        ReceiptImageUrl = expense.ReceiptImageUrl,
        IsRecurring = expense.IsRecurring,
        RecurrenceInterval = expense.RecurrenceInterval
    };
}
