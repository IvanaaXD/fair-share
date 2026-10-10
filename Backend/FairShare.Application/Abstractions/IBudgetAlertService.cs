namespace FairShare.Application.Interfaces;

/// <summary>
/// Budget threshold notifications (80% and 100% of the monthly limit). Used for expenses added
/// by the user and for copies of recurring expenses added by the background job.
/// </summary>
public interface IBudgetAlertService
{
    /// <summary>
    /// Spending in the category in the month of <paramref name="date"/>. Must be read BEFORE the
    /// new expense is saved - it is the baseline for detecting that a threshold was just crossed.
    /// </summary>
    Task<decimal> GetSpentInMonthAsync(
        Guid userId, Guid categoryId, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>Notifies the user if adding <paramref name="amount"/> to <paramref name="spentBefore"/> crossed a threshold.</summary>
    Task NotifyIfThresholdCrossedAsync(
        Guid userId,
        Guid categoryId,
        string categoryName,
        string currency,
        DateTime date,
        decimal spentBefore,
        decimal amount,
        CancellationToken cancellationToken = default);
}
