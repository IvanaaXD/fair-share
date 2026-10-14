using AutoMapper;
using FairShare.Application.Abstractions;
using FairShare.Application.Common.Currency;
using FairShare.Application.DTOs.Analytics;
using FairShare.Application.Interfaces;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Models;

namespace FairShare.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    /// <summary>Upper limit of map points, so the response cannot grow without bound.</summary>
    private const int MaxMapPoints = 500;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IExchangeRateProvider _exchangeRates;
    private readonly IMapper _mapper;

    public AnalyticsService(IUnitOfWork unitOfWork, IExchangeRateProvider exchangeRates, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _exchangeRates = exchangeRates;
        _mapper = mapper;
    }

    public async Task<SpendingAnalyticsResponse> GetSpendingAnalyticsAsync(
        Guid currentUserId,
        AnalyticsPeriod period,
        DateTime? from,
        DateTime? to,
        string? currency,
        CancellationToken cancellationToken = default)
    {
        var range = AnalyticsPeriodResolver.Resolve(period, from, to, DateTime.UtcNow);
        var previous = AnalyticsPeriodResolver.GetPreviousRange(range);
        var granularity = AnalyticsPeriodResolver.GetGranularity(range);

        // Everything is shown in one currency: the one asked for, otherwise the user's default.
        // Expenses in other currencies are converted with today's rates instead of being added
        // as they are (50 KM + 20 € is not 70 of anything).
        var target = await ResolveCurrencyAsync(currentUserId, currency, cancellationToken);
        var rates = await _exchangeRates.GetRatesAsync(cancellationToken);

        // Aggregation happens in the database: one row per day/category AND currency.
        var daily = await _unitOfWork.Expenses.GetDailyTotalsAsync(
            currentUserId, range.From, range.ToExclusive, null, cancellationToken);
        var categories = await _unitOfWork.Expenses.GetTotalsByCategoryAsync(
            currentUserId, range.From, range.ToExclusive, null, cancellationToken);
        var previousCategories = await _unitOfWork.Expenses.GetTotalsByCategoryAsync(
            currentUserId, previous.From, previous.ToExclusive, null, cancellationToken);

        var convertedDaily = new List<(DateTime Day, decimal Total)>();
        int count = 0, converted = 0, unconverted = 0;
        foreach (var row in daily)
        {
            if (!rates.TryConvert(row.Total, row.Currency, target, out var amount))
            {
                unconverted += row.Count;
                continue;
            }

            convertedDaily.Add((row.Day, amount));
            count += row.Count;
            if (!SameCurrency(row.Currency, target))
                converted += row.Count;
        }

        var byCategory = SumByCategory(categories, rates, target);
        var previousByCategory = SumByCategory(previousCategories, rates, target);

        var total = convertedDaily.Sum(d => d.Total);
        var previousTotal = previousByCategory.Values.Sum(c => c.Total);

        // The response consists of calculated values, so it is built by hand rather than mapped.
        return new SpendingAnalyticsResponse
        {
            From = range.From,
            To = range.To,
            Granularity = granularity,
            Currency = target,
            TotalSpent = total,
            ExpenseCount = count,
            ConvertedExpenseCount = converted,
            UnconvertedExpenseCount = unconverted,
            RatesUpdatedAt = converted > 0 ? rates.UpdatedAt : null,
            PreviousFrom = previous.From,
            PreviousTo = previous.To,
            PreviousPeriodTotal = previousTotal,
            ChangePercentage = previousTotal > 0
                ? Math.Round((total - previousTotal) / previousTotal * 100m, 2)
                : null,
            Timeline = BuildTimeline(convertedDaily, range, granularity),
            ByCategory = byCategory.Values
                .OrderByDescending(c => c.Total)
                .Select(c => new CategorySpendingResponse
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Total = c.Total,
                    Count = c.Count,
                    Percentage = total > 0 ? Math.Round(c.Total / total * 100m, 2) : 0m,
                    PreviousPeriodTotal = previousByCategory.TryGetValue(c.CategoryId, out var before) ? before.Total : 0m
                })
                .ToList()
        };
    }

    public async Task<IReadOnlyList<ExpenseLocationResponse>> GetExpenseLocationsAsync(
        Guid currentUserId,
        AnalyticsPeriod period,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var range = AnalyticsPeriodResolver.Resolve(period, from, to, DateTime.UtcNow);

        var locations = await _unitOfWork.Expenses.GetLocationsAsync(
            currentUserId, range.From, range.ToExclusive, MaxMapPoints, cancellationToken);

        return _mapper.Map<List<ExpenseLocationResponse>>(locations);
    }

    private async Task<string> ResolveCurrencyAsync(Guid userId, string? requested, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(requested))
            return requested.Trim().ToUpperInvariant();

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        return user?.DefaultCurrency ?? "BAM";
    }

    private static bool SameCurrency(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private sealed record CategorySum(Guid CategoryId, string CategoryName, decimal Total, int Count);

    /// <summary>Category rows of all currencies, converted and added up per category.</summary>
    private static Dictionary<Guid, CategorySum> SumByCategory(
        IReadOnlyList<CategorySpendingTotal> rows, ExchangeRates rates, string target)
    {
        var result = new Dictionary<Guid, CategorySum>();
        foreach (var row in rows)
        {
            if (!rates.TryConvert(row.Total, row.Currency, target, out var amount))
                continue;

            result[row.CategoryId] = result.TryGetValue(row.CategoryId, out var sum)
                ? sum with { Total = sum.Total + amount, Count = sum.Count + row.Count }
                : new CategorySum(row.CategoryId, row.CategoryName, amount, row.Count);
        }

        return result;
    }

    /// <summary>
    /// Turns daily totals into chart points at the chosen granularity. Empty days/weeks/months
    /// get 0 so the chart is continuous; the first and last bucket are clipped to the period.
    /// </summary>
    private static List<SpendingTimePoint> BuildTimeline(
        IReadOnlyList<(DateTime Day, decimal Total)> daily,
        DateRange range,
        TimeGranularity granularity)
    {
        var totalsByBucket = daily
            .GroupBy(d => AnalyticsPeriodResolver.BucketStart(d.Day, granularity))
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Total));

        var points = new List<SpendingTimePoint>();
        for (var bucket = AnalyticsPeriodResolver.BucketStart(range.From, granularity);
             bucket <= range.To;
             bucket = AnalyticsPeriodResolver.NextBucket(bucket, granularity))
        {
            var bucketEnd = AnalyticsPeriodResolver.NextBucket(bucket, granularity).AddDays(-1);

            points.Add(new SpendingTimePoint
            {
                PeriodStart = bucket < range.From ? range.From : bucket,
                PeriodEnd = bucketEnd > range.To ? range.To : bucketEnd,
                Total = totalsByBucket.GetValueOrDefault(bucket, 0m)
            });
        }

        return points;
    }
}
