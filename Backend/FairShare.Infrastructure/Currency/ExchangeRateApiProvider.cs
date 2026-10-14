using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FairShare.Application.Abstractions;
using FairShare.Application.Common.Currency;
using Microsoft.Extensions.Logging;

namespace FairShare.Infrastructure.Currency;

/// <summary>
/// Rates from ExchangeRate-API's open endpoint (https://open.er-api.com/v6/latest/EUR): free,
/// no API key, updated once a day. Their terms ask for a link to them where the rates are
/// shown - the web app shows it under the analytics.
///
/// Loaded rates are kept in memory for 6 hours and shared by all requests. If the service is
/// down, the last loaded rates are used; before the first successful load only the fixed
/// EUR/BAM rate is known (other currencies are then left out of totals).
/// </summary>
public sealed class ExchangeRateApiProvider : IExchangeRateProvider
{
    public const string Url = "https://open.er-api.com/v6/latest/EUR";

    private static readonly TimeSpan KeepFor = TimeSpan.FromHours(6);
    private static readonly TimeSpan RetryAfterFailure = TimeSpan.FromMinutes(10);
    private static readonly SemaphoreSlim Lock = new(1, 1);

    // Shared by all instances (the class is created per request by the HttpClient factory).
    private static ExchangeRates? _cached;
    private static DateTime _nextLoadAt = DateTime.MinValue;

    private readonly HttpClient _http;
    private readonly ILogger<ExchangeRateApiProvider> _logger;

    public ExchangeRateApiProvider(HttpClient http, ILogger<ExchangeRateApiProvider> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ExchangeRates> GetRatesAsync(CancellationToken cancellationToken = default)
    {
        if (_cached is not null && DateTime.UtcNow < _nextLoadAt)
            return _cached;

        await Lock.WaitAsync(cancellationToken);
        try
        {
            // Another request may have loaded them while this one was waiting.
            if (_cached is not null && DateTime.UtcNow < _nextLoadAt)
                return _cached;

            try
            {
                var response = await _http.GetFromJsonAsync<RatesResponse>(Url, cancellationToken);
                if (response is { Result: "success", Rates.Count: > 0 })
                {
                    var updatedAt = DateTimeOffset.FromUnixTimeSeconds(response.TimeLastUpdateUnix).UtcDateTime;
                    _cached = new ExchangeRates(response.Rates, updatedAt, "exchangerate-api");
                    _nextLoadAt = DateTime.UtcNow.Add(KeepFor);
                    return _cached;
                }

                _logger.LogWarning("Exchange rate service returned no rates.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                _logger.LogWarning(ex, "Exchange rates could not be loaded; using the last known rates.");
            }

            _nextLoadAt = DateTime.UtcNow.Add(RetryAfterFailure);
            return _cached ?? ExchangeRates.FixedOnly;
        }
        finally
        {
            Lock.Release();
        }
    }

    private sealed class RatesResponse
    {
        [JsonPropertyName("result")]
        public string? Result { get; set; }

        [JsonPropertyName("time_last_update_unix")]
        public long TimeLastUpdateUnix { get; set; }

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}
