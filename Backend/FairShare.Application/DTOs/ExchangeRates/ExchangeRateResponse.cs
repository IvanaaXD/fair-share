namespace FairShare.Application.DTOs.ExchangeRates;

/// <summary>Today's rate between two currencies (GET api/exchange-rates).</summary>
/// <param name="Rate">Units of <paramref name="To"/> for one unit of <paramref name="From"/>.</param>
/// <param name="UpdatedAt">When the rates were published; null for the fixed EUR/BAM rate.</param>
public sealed record ExchangeRateResponse(string From, string To, decimal Rate, DateTime? UpdatedAt, string Source);
