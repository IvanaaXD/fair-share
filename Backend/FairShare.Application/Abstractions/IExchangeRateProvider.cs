using FairShare.Application.Common.Currency;

namespace FairShare.Application.Abstractions;

/// <summary>
/// Today's exchange rates. The implementation (Infrastructure) loads them from a free rate
/// service at most a few times a day and keeps them in memory. It never throws: when the
/// service cannot be reached it returns the last rates it had, or at least the fixed EUR/BAM rate.
/// </summary>
public interface IExchangeRateProvider
{
    Task<ExchangeRates> GetRatesAsync(CancellationToken cancellationToken = default);
}
