using FairShare.Application.Abstractions;
using FairShare.Application.Common.Notifications;
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
    private static readonly decimal[] BudgetThresholds = { 1.0m, 0.8m };

    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly IExchangeRateProvider _exchangeRates;

    public BudgetAlertService(
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IExchangeRateProvider exchangeRates)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _exchangeRates = exchangeRates;
    }

    /// <summary>Spending in the category that month, in the user's default currency (the budget's currency).</summary>
    public async Task<decimal> GetSpentInMonthAsync(
        Guid userId, Guid categoryId, DateTime date, CancellationToken cancellationToken = default)
    {
        var (monthStart, monthEnd) = GetMonth(date);
        var totals = await _unitOfWork.Expenses.GetTotalsByCurrencyAsync(
            userId, categoryId, monthStart, monthEnd, cancellationToken);
        var rates = await _exchangeRates.GetRatesAsync(cancellationToken);
        return rates.SumIn(await UserCurrencyAsync(userId, cancellationToken), totals);
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

        // The new expense may be in another currency than the budget (the user's default one).
        var budgetCurrency = await UserCurrencyAsync(userId, cancellationToken);
        var rates = await _exchangeRates.GetRatesAsync(cancellationToken);
        if (!rates.TryConvert(amount, currency, budgetCurrency, out var convertedAmount))
            return;

        var spentAfter = spentBefore + convertedAmount;

        foreach (var fraction in BudgetThresholds)
        {
            var limitAtThreshold = budget.MonthlyLimit * fraction;

            // Notify only at the moment a threshold is crossed, not for every expense above it.
            if (spentBefore < limitAtThreshold && spentAfter >= limitAtThreshold)
            {
                var category = NotificationText.Quote(categoryName);
                var period = NotificationText.Month(date);
                var limit = NotificationText.Money(budget.MonthlyLimit, budgetCurrency);

                var (subject, message) = fraction >= 1.0m
                    ? ("Буџет је потрошен",
                        $"Буџет за категорију {category} за {period} је потрошен: достигнуто је 100 % лимита од {limit}.")
                    : ("Буџет: 80 % лимита",
                        $"Потрошено је 80 % буџета за категорију {category} за {period} (лимит {limit}).");

                await _notificationService.NotifyAsync(
                    userId, NotificationType.BudgetExceeded, subject, message, cancellationToken);
                return;
            }
        }
    }

    private async Task<string> UserCurrencyAsync(Guid userId, CancellationToken cancellationToken)
        => (await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken))?.DefaultCurrency ?? "BAM";

    private static (DateTime Start, DateTime End) GetMonth(DateTime date)
    {
        var start = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddMonths(1).AddTicks(-1));
    }
}
