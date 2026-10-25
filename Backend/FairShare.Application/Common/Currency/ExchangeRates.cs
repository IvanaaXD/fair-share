using FairShare.Domain.Models;

namespace FairShare.Application.Common.Currency;

/// <summary>
/// Exchange rates of one day, all relative to the euro (1 EUR = PerEuro[code]). Converting
/// between two currencies goes through the euro: 100 RSD -> EUR -> BAM.
/// </summary>
public sealed class ExchangeRates
{
    /// <summary>
    /// The convertible mark is tied to the euro by law (currency board): 1 EUR = 1.95583 BAM.
    /// This rate is always known, even when the rate service cannot be reached.
    /// </summary>
    public const decimal BamPerEuro = 1.95583m;

    private readonly IReadOnlyDictionary<string, decimal> _perEuro;

    public ExchangeRates(IReadOnlyDictionary<string, decimal> perEuro, DateTime? updatedAt, string source)
    {
        var rates = new Dictionary<string, decimal>(perEuro, StringComparer.OrdinalIgnoreCase)
        {
            ["EUR"] = 1m,
            ["BAM"] = BamPerEuro
        };
        _perEuro = rates;
        UpdatedAt = updatedAt;
        Source = source;
    }

    /// <summary>Only the fixed EUR/BAM rate - used when no rates could be loaded yet.</summary>
    public static ExchangeRates FixedOnly { get; } = new(new Dictionary<string, decimal>(), null, "fixed");

    /// <summary>When the rates were published (null for the fixed EUR/BAM rate only).</summary>
    public DateTime? UpdatedAt { get; }

    /// <summary>"exchangerate-api" or "fixed".</summary>
    public string Source { get; }

    /// <summary>Units of each known currency per euro (1 EUR = value); always has EUR and BAM.</summary>
    public IReadOnlyDictionary<string, decimal> PerEuro => _perEuro;

    public bool TryConvert(decimal amount, string fromCurrency, string toCurrency, out decimal converted)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
        {
            converted = amount;
            return true;
        }

        if (_perEuro.TryGetValue(fromCurrency, out var fromRate) && fromRate > 0 &&
            _perEuro.TryGetValue(toCurrency, out var toRate) && toRate > 0)
        {
            converted = Math.Round(amount / fromRate * toRate, 2);
            return true;
        }

        converted = 0m;
        return false;
    }

    /// <summary>
    /// How many units of <paramref name="toCurrency"/> one unit of <paramref name="fromCurrency"/>
    /// is worth (8 decimal places). False if the rate of either currency is not known.
    /// </summary>
    public bool TryGetRate(string fromCurrency, string toCurrency, out decimal rate)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
        {
            rate = 1m;
            return true;
        }

        if (_perEuro.TryGetValue(fromCurrency, out var fromRate) && fromRate > 0 &&
            _perEuro.TryGetValue(toCurrency, out var toRate) && toRate > 0)
        {
            rate = Math.Round(toRate / fromRate, 8);
            return true;
        }

        rate = 0m;
        return false;
    }

    /// <summary>
    /// Adds up totals in different currencies, converted to <paramref name="targetCurrency"/>.
    /// Totals whose currency has no known rate are left out.
    /// </summary>
    public decimal SumIn(string targetCurrency, IEnumerable<CurrencyTotal> totals)
    {
        var sum = 0m;
        foreach (var total in totals)
        {
            if (TryConvert(total.Total, total.Currency, targetCurrency, out var amount))
                sum += amount;
        }

        return sum;
    }
}
