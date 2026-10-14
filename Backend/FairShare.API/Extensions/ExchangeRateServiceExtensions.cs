using FairShare.Application.Abstractions;
using FairShare.Infrastructure.Currency;

namespace FairShare.WebAPI.Extensions;

public static class ExchangeRateServiceExtensions
{
    /// <summary>Exchange rates for converting expenses in other currencies (analytics, budgets).</summary>
    public static IServiceCollection AddExchangeRates(this IServiceCollection services)
    {
        // A short timeout: if the rate service is slow, the app uses the last known rates
        // instead of making the user wait.
        services.AddHttpClient<IExchangeRateProvider, ExchangeRateApiProvider>(client =>
            client.Timeout = TimeSpan.FromSeconds(5));
        return services;
    }
}
