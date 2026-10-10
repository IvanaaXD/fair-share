using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Services;

/// <summary>
/// Dates of a recurring expense. Every occurrence is counted from the FIRST expense (the anchor),
/// never from the previous occurrence: a monthly expense from 31 January falls on 28 February,
/// then again on 31 March - adding one month to the previous date would get stuck on the 28th.
/// </summary>
public static class RecurrenceSchedule
{
    /// <summary>The anchor moved forward by the given number of intervals.</summary>
    public static DateTime AddIntervals(DateTime anchor, RecurrenceInterval interval, int count) => interval switch
    {
        RecurrenceInterval.Daily => anchor.AddDays(count),
        RecurrenceInterval.Weekly => anchor.AddDays(7 * count),
        RecurrenceInterval.Monthly => anchor.AddMonths(count),
        RecurrenceInterval.Yearly => anchor.AddYears(count),
        _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, "Expense does not repeat.")
    };

    /// <summary>First occurrence (anchor + k intervals, k ≥ 1) that falls strictly after <paramref name="after"/>.</summary>
    public static DateTime NextOccurrenceAfter(DateTime anchor, RecurrenceInterval interval, DateTime after)
    {
        // Start just below the answer instead of at 1, so a daily expense that started years ago
        // does not need thousands of iterations. The estimate is never past the answer.
        var count = Math.Max(1, EstimateIntervalsBetween(anchor, after, interval) - 1);

        while (AddIntervals(anchor, interval, count) <= after)
            count++;

        return AddIntervals(anchor, interval, count);
    }

    private static int EstimateIntervalsBetween(DateTime from, DateTime to, RecurrenceInterval interval)
    {
        if (to <= from)
            return 0;

        return interval switch
        {
            RecurrenceInterval.Daily => (int)(to - from).TotalDays,
            RecurrenceInterval.Weekly => (int)((to - from).TotalDays / 7),
            RecurrenceInterval.Monthly => (to.Year - from.Year) * 12 + to.Month - from.Month,
            RecurrenceInterval.Yearly => to.Year - from.Year,
            _ => 0
        };
    }
}
