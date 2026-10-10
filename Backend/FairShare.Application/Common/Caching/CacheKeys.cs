using System.Globalization;
using FairShare.Application.DTOs.Analytics;

namespace FairShare.Application.Common.Caching;

/// <summary>
/// All cache keys in one place. Every key with personal data contains the user id, so one
/// user's cached data can never be returned to another user.
/// </summary>
public static class CacheKeys
{
    public const string AllCategories = "categories:all";

    public static string AccountStatus(Guid userId) => $"account-status:{userId:N}";

    public static string SpendingAnalytics(Guid userId, AnalyticsPeriod period, DateTime? from, DateTime? to)
        => $"analytics:spending:{userId:N}:{period}:{Format(from)}:{Format(to)}";

    public static string ExpenseLocations(Guid userId, AnalyticsPeriod period, DateTime? from, DateTime? to)
        => $"analytics:locations:{userId:N}:{period}:{Format(from)}:{Format(to)}";

    public static string AdminStatistics(AnalyticsPeriod period, DateTime? from, DateTime? to)
        => $"admin:statistics:{period}:{Format(from)}:{Format(to)}";

    private static string Format(DateTime? value)
        => value?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) ?? "-";
}

/// <summary>
/// Tags group entries that become outdated together. They are invalidated automatically by
/// CacheInvalidationInterceptor whenever the related rows are saved - no service has to remember it.
/// </summary>
public static class CacheTags
{
    /// <summary>Category list, and analytics (they show category names).</summary>
    public const string Categories = "categories";

    /// <summary>Everything calculated from a user's personal expenses (analytics, map).</summary>
    public static string UserSpending(Guid userId) => $"user-spending:{userId:N}";

    /// <summary>A user's account state (blocked, must change password).</summary>
    public static string User(Guid userId) => $"user:{userId:N}";
}

/// <summary>
/// How long entries live at most. Invalidation by tag removes outdated entries sooner; these
/// limits matter for data that changes without a save we can see, e.g. "last week" moving on.
/// </summary>
public static class CacheDurations
{
    public static readonly TimeSpan Categories = TimeSpan.FromHours(1);
    public static readonly TimeSpan Analytics = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan AccountStatus = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Admin statistics count rows across the whole system and are not invalidated on every save
    /// (that would happen on almost every request); they may be up to 2 minutes old.
    /// </summary>
    public static readonly TimeSpan AdminStatistics = TimeSpan.FromMinutes(2);
}
