using FairShare.Application.DTOs.Analytics;
using FairShare.Application.Common.Exceptions;

namespace FairShare.Application.Services;

/// <summary>Period by days: From and To are inclusive (both at 00:00 UTC).</summary>
public record DateRange(DateTime From, DateTime To)
{
    public DateTime ToExclusive => To.AddDays(1);
    public int Days => (To - From).Days + 1;
}

/// <summary>
/// Pure logic for calculating the analytics period (feature 5.4), extracted into a
/// static class to be easily covered by unit tests.
/// </summary>
public static class AnalyticsPeriodResolver
{
    private const int MaxDaysForDaily = 31;
    private const int MaxDaysForWeekly = 92;

    public static DateRange Resolve(AnalyticsPeriod period, DateTime? from, DateTime? to, DateTime nowUtc)
    {
        var today = Utc(nowUtc.Date);

        return period switch
        {
            AnalyticsPeriod.LastWeek => new DateRange(today.AddDays(-6), today),
            AnalyticsPeriod.LastMonth => new DateRange(today.AddMonths(-1).AddDays(1), today),
            AnalyticsPeriod.Last3Months => new DateRange(today.AddMonths(-3).AddDays(1), today),
            AnalyticsPeriod.Last6Months => new DateRange(today.AddMonths(-6).AddDays(1), today),
            AnalyticsPeriod.LastYear => new DateRange(today.AddYears(-1).AddDays(1), today),
            AnalyticsPeriod.Custom => ResolveCustom(from, to),
            _ => throw new ConflictException("Unknown analytics period.")
        };
    }

    /// <summary>Previous period of the same length, ending the day before the start of the selected one.</summary>
    public static DateRange GetPreviousRange(DateRange range)
        => new(range.From.AddDays(-range.Days), range.From.AddDays(-1));

    /// <summary>Shorter periods are displayed by days, medium by weeks, longer by months.</summary>
    public static TimeGranularity GetGranularity(DateRange range) => range.Days switch
    {
        <= MaxDaysForDaily => TimeGranularity.Day,
        <= MaxDaysForWeekly => TimeGranularity.Week,
        _ => TimeGranularity.Month
    };

    /// <summary>Start of the "bucket" to which the day belongs; the week starts on Monday.</summary>
    public static DateTime BucketStart(DateTime day, TimeGranularity granularity)
    {
        var date = Utc(day.Date);
        return granularity switch
        {
            TimeGranularity.Day => date,
            TimeGranularity.Week => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
            TimeGranularity.Month => new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => date
        };
    }

    public static DateTime NextBucket(DateTime bucketStart, TimeGranularity granularity) => granularity switch
    {
        TimeGranularity.Day => bucketStart.AddDays(1),
        TimeGranularity.Week => bucketStart.AddDays(7),
        TimeGranularity.Month => bucketStart.AddMonths(1),
        _ => bucketStart.AddDays(1)
    };

    private static DateRange ResolveCustom(DateTime? from, DateTime? to)
    {
        if (from is null || to is null)
            throw new ConflictException("For a custom period, from and to dates must be specified.");

        var start = Utc(from.Value.Date);
        var end = Utc(to.Value.Date);

        if (start > end)
            throw new ConflictException("The start date cannot be after the end date.");

        if (end > start.AddYears(1))
            throw new ConflictException("The difference between dates cannot be greater than one year.");

        return new DateRange(start, end);
    }

    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}