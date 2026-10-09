using FairShare.Application.DTOs.Analytics;
using FairShare.Application.Common.Exceptions;

namespace FairShare.Application.Services;

/// <summary>Период по данима: From и To су укључени (обоје у 00:00 UTC).</summary>
public record DateRange(DateTime From, DateTime To)
{
    public DateTime ToExclusive => To.AddDays(1);
    public int Days => (To - From).Days + 1;
}

/// <summary>
/// Чиста логика за рачунање периода аналитике (функционалност 5.4), издвојена у
/// статичку класу да би се лако покрила јединичним тестовима.
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
            _ => throw new ConflictException("Непознат период аналитике.")
        };
    }

    /// <summary>Претходни период исте дужине, који се завршава дан прије почетка изабраног.</summary>
    public static DateRange GetPreviousRange(DateRange range)
        => new(range.From.AddDays(-range.Days), range.From.AddDays(-1));

    /// <summary>Краћи периоди се приказују по данима, средњи по седмицама, дужи по мјесецима.</summary>
    public static TimeGranularity GetGranularity(DateRange range) => range.Days switch
    {
        <= MaxDaysForDaily => TimeGranularity.Day,
        <= MaxDaysForWeekly => TimeGranularity.Week,
        _ => TimeGranularity.Month
    };

    /// <summary>Почетак "кантице" којој дан припада; седмица почиње понедјељком.</summary>
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
            throw new ConflictException("За произвољан период потребно је задати датуме од и до.");

        var start = Utc(from.Value.Date);
        var end = Utc(to.Value.Date);

        if (start > end)
            throw new ConflictException("Датум почетка не смије бити послије датума краја.");

        if (end > start.AddYears(1))
            throw new ConflictException("Разлика између датума не смије бити већа од годину дана.");

        return new DateRange(start, end);
    }

    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
