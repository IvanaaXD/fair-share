using System.Globalization;

namespace FairShare.Domain.Entities;

public class Budget : BaseEntity
{
    /// <summary>A budget can be planned at most this many months ahead.</summary>
    public const int MaxMonthsAhead = 12;

    /// <summary>
    /// The server works in UTC, users in their own time zone (up to UTC+14). A month is treated
    /// as finished only when it has finished everywhere, so nobody loses the last hours of it.
    /// </summary>
    private static readonly TimeSpan TimeZoneTolerance = TimeSpan.FromHours(14);

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public decimal MonthlyLimit { get; set; }

    /// <summary>"YYYY-MM"</summary>
    public string Month { get; set; } = string.Empty;

    public bool CheckThreshold(decimal currentSpending, double thresholdPercentage = 0.8)
        => currentSpending >= MonthlyLimit * (decimal)thresholdPercentage;

    /// <summary>The month is over: its budgets are history and can only be viewed.</summary>
    public bool IsClosed(DateTime utcNow) => IsMonthClosed(Month, utcNow);

    /// <summary>"2026-10" -> 1 October 2026, 00:00 UTC.</summary>
    public static DateTime MonthStart(string month)
        => DateTime.SpecifyKind(
            DateTime.ParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture),
            DateTimeKind.Utc);

    public static bool IsMonthClosed(string month, DateTime utcNow)
        => utcNow >= MonthStart(month).AddMonths(1) + TimeZoneTolerance;

    public static bool IsMonthTooFarAhead(string month, DateTime utcNow)
    {
        var latest = utcNow + TimeZoneTolerance;
        var lastAllowed = new DateTime(latest.Year, latest.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(MaxMonthsAhead);
        return MonthStart(month) > lastAllowed;
    }
}
