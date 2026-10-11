using FairShare.Domain.Models;

namespace FairShare.Domain.Interfaces;

/// <summary>
/// Read-only queries over multiple entities simultaneously (feature 5.11).
/// Not part of the IUnitOfWork because it never modifies data.
/// </summary>
public interface IStatisticsRepository
{
    Task<SystemTotals> GetTotalsAsync(CancellationToken cancellationToken = default);

    /// <summary>Number of new records for a given series by day, in the interval [from, toExclusive).</summary>
    Task<IReadOnlyList<DailyCount>> GetDailyCountsAsync(
        StatisticSeries series,
        DateTime from,
        DateTime toExclusive,
        CancellationToken cancellationToken = default);
}