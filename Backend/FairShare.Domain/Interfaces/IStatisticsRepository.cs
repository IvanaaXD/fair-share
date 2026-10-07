using FairShare.Domain.Models;

namespace FairShare.Domain.Interfaces;

/// <summary>
/// Упити само за читање над више ентитета истовремено (функционалност 5.11).
/// Није дио IUnitOfWork-а јер никада не мијења податке.
/// </summary>
public interface IStatisticsRepository
{
    Task<SystemTotals> GetTotalsAsync(CancellationToken cancellationToken = default);

    /// <summary>Број нових записа дате серије по данима, у интервалу [from, toExclusive).</summary>
    Task<IReadOnlyList<DailyCount>> GetDailyCountsAsync(
        StatisticSeries series,
        DateTime from,
        DateTime toExclusive,
        CancellationToken cancellationToken = default);
}
