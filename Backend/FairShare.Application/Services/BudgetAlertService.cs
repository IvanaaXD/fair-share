using FairShare.Application.Interfaces;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;

namespace FairShare.Application.Services;

/// <summary>
/// Moved here from ExpenseService without changes in behaviour, so the background job for
/// recurring expenses sends exactly the same budget notifications as a manually added expense.
/// </summary>
public class BudgetAlertService : IBudgetAlertService
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

    public BudgetAlertService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<decimal> GetSpentInMonthAsync(
        Guid userId, Guid categoryId, DateTime date, CancellationToken cancellationToken = default)
    {
        var (monthStart, monthEnd) = GetMonth(date);
        return await _unitOfWork.Expenses.GetTotalByUserAndCategoryAsync(
            userId, categoryId, monthStart, monthEnd, cancellationToken);
    }

    public async Task NotifyIfThresholdCrossedAsync(
        Guid userId,
        Guid categoryId,
        string categoryName,
        string currency,
        DateTime date,
        decimal spentBefore,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var month = date.ToString("yyyy-MM");
        var budget = await _unitOfWork.Budgets.GetByUserCategoryAndMonthAsync(userId, categoryId, month, cancellationToken);
        if (budget is null || budget.MonthlyLimit <= 0)
            return;

        var spentAfter = spentBefore + amount;

        foreach (var (fraction, label) in BudgetThresholds)
        {
            var limitAtThreshold = budget.MonthlyLimit * fraction;

            // Notify only at the moment a threshold is crossed, not for every expense above it.
            if (spentBefore < limitAtThreshold && spentAfter >= limitAtThreshold)
            {
                await _notificationService.NotifyAsync(
                    userId,
                    NotificationType.BudgetExceeded,
                    "Прекорачење буџета",
                    $"Потрошња у категорији '{categoryName}' достигла је {label} мјесечног " +
                    $"лимита од {budget.MonthlyLimit} {currency} за {month}.",
                    cancellationToken);
                return;
            }
        }
    }

    private static (DateTime Start, DateTime End) GetMonth(DateTime date)
    {
        var start = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddMonths(1).AddTicks(-1));
    }
}
