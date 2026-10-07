using FairShare.Application.DTOs.Admin;
using FairShare.Application.DTOs.Analytics;
using FairShare.Application.Interfaces;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Models;

namespace FairShare.Application.Services;

public class AdminStatisticsService : IAdminStatisticsService
{
    private readonly IStatisticsRepository _statistics;

    public AdminStatisticsService(IStatisticsRepository statistics)
    {
        _statistics = statistics;
    }

    public async Task<AdminStatisticsResponse> GetStatisticsAsync(
        AnalyticsPeriod period,
        DateTime? from,
        DateTime? to,
        CancellationToken cancellationToken = default)
    {
        // Исти избор периода и грануларности као у личној аналитици (5.4).
        var range = AnalyticsPeriodResolver.Resolve(period, from, to, DateTime.UtcNow);
        var granularity = AnalyticsPeriodResolver.GetGranularity(range);

        var totals = await _statistics.GetTotalsAsync(cancellationToken);

        // За сваку серију: број по данима из базе, затим груписање у кантице изабране грануларности.
        var bucketsBySeries = new Dictionary<StatisticSeries, Dictionary<DateTime, int>>();
        foreach (var series in Enum.GetValues<StatisticSeries>())
        {
            var daily = await _statistics.GetDailyCountsAsync(series, range.From, range.ToExclusive, cancellationToken);
            bucketsBySeries[series] = daily
                .GroupBy(d => AnalyticsPeriodResolver.BucketStart(d.Day, granularity))
                .ToDictionary(g => g.Key, g => g.Sum(d => d.Count));
        }

        int CountFor(StatisticSeries series, DateTime bucket)
            => bucketsBySeries[series].GetValueOrDefault(bucket, 0);

        var timeline = new List<AdminTimePoint>();
        for (var bucket = AnalyticsPeriodResolver.BucketStart(range.From, granularity);
             bucket <= range.To;
             bucket = AnalyticsPeriodResolver.NextBucket(bucket, granularity))
        {
            var bucketEnd = AnalyticsPeriodResolver.NextBucket(bucket, granularity).AddDays(-1);

            timeline.Add(new AdminTimePoint
            {
                PeriodStart = bucket < range.From ? range.From : bucket,
                PeriodEnd = bucketEnd > range.To ? range.To : bucketEnd,
                NewUsers = CountFor(StatisticSeries.NewUsers, bucket),
                NewGroups = CountFor(StatisticSeries.NewGroups, bucket),
                PersonalExpenses = CountFor(StatisticSeries.PersonalExpenses, bucket),
                GroupExpenses = CountFor(StatisticSeries.GroupExpenses, bucket),
                Settlements = CountFor(StatisticSeries.Settlements, bucket),
                Payments = CountFor(StatisticSeries.Payments, bucket)
            });
        }

        return new AdminStatisticsResponse
        {
            From = range.From,
            To = range.To,
            Granularity = granularity,
            Totals = new AdminTotalsResponse
            {
                Users = totals.Users,
                BlockedUsers = totals.BlockedUsers,
                Admins = totals.Admins,
                Groups = totals.Groups,
                PersonalExpenses = totals.PersonalExpenses,
                GroupExpenses = totals.GroupExpenses,
                ProposedSettlements = totals.ProposedSettlements,
                SettledSettlements = totals.SettledSettlements,
                PaymentsByStatus = totals.PaymentsByStatus.ToDictionary(p => p.Key.ToString(), p => p.Value)
            },
            Timeline = timeline
        };
    }
}
