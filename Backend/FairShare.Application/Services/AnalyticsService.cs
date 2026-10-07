using FairShare.Application.DTOs.Analytics;
using FairShare.Application.Interfaces;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Models;

namespace FairShare.Application.Services;

public class AnalyticsService : IAnalyticsService
{
    /// <summary>Горња граница тачака на мапи, да одговор не порасте неограничено.</summary>
    private const int MaxMapPoints = 500;

    private readonly IUnitOfWork _unitOfWork;

    public AnalyticsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SpendingAnalyticsResponse> GetSpendingAnalyticsAsync(
        Guid currentUserId,
        AnalyticsPeriod period,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var range = AnalyticsPeriodResolver.Resolve(period, from, to, DateTime.UtcNow);
        var previous = AnalyticsPeriodResolver.GetPreviousRange(range);
        var granularity = AnalyticsPeriodResolver.GetGranularity(range);

        // Све агрегације се раде у бази; у меморију долази највише по један ред по дану/категорији.
        var daily = await _unitOfWork.Expenses.GetDailyTotalsAsync(
            currentUserId, range.From, range.ToExclusive, cancellationToken);
        var categories = await _unitOfWork.Expenses.GetTotalsByCategoryAsync(
            currentUserId, range.From, range.ToExclusive, cancellationToken);
        var previousCategories = await _unitOfWork.Expenses.GetTotalsByCategoryAsync(
            currentUserId, previous.From, previous.ToExclusive, cancellationToken);

        var total = daily.Sum(d => d.Total);
        var previousTotal = previousCategories.Sum(c => c.Total);
        var previousByCategory = previousCategories.ToDictionary(c => c.CategoryId, c => c.Total);

        return new SpendingAnalyticsResponse
        {
            From = range.From,
            To = range.To,
            Granularity = granularity,
            TotalSpent = total,
            ExpenseCount = daily.Sum(d => d.Count),
            PreviousFrom = previous.From,
            PreviousTo = previous.To,
            PreviousPeriodTotal = previousTotal,
            ChangePercentage = previousTotal > 0
                ? Math.Round((total - previousTotal) / previousTotal * 100m, 2)
                : null,
            Timeline = BuildTimeline(daily, range, granularity),
            ByCategory = categories
                .OrderByDescending(c => c.Total)
                .Select(c => new CategorySpendingResponse
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    Total = c.Total,
                    Count = c.Count,
                    Percentage = total > 0 ? Math.Round(c.Total / total * 100m, 2) : 0m,
                    PreviousPeriodTotal = previousByCategory.GetValueOrDefault(c.CategoryId, 0m)
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

        return locations.Select(l => new ExpenseLocationResponse
        {
            ExpenseId = l.Id,
            Amount = l.Amount,
            Currency = l.Currency,
            Date = l.Date,
            Description = l.Description,
            CategoryName = l.CategoryName,
            Latitude = l.Latitude,
            Longitude = l.Longitude
        }).ToList();
    }

    /// <summary>
    /// Од дневних сума прави низ тачака у изабраној грануларности. Празни дани/седмице/мјесеци
    /// добијају 0, да график буде непрекидан. Прва и посљедња кантица се сијеку са границама периода.
    /// </summary>
    private static List<SpendingTimePoint> BuildTimeline(
        IReadOnlyList<DailySpendingTotal> daily,
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
