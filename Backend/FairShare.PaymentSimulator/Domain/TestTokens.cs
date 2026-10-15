using System.Text.RegularExpressions;

namespace FairShare.PaymentSimulator.Domain;

/// <summary>
/// Ready-made test tokens, like a real provider's sandbox ("tok_visa" and similar), so demo
/// data can contain saved cards without anyone typing a card number:
///     tok_test_{outcome}_{random}     e.g. tok_test_ok_3f9a..., tok_test_insufficient_funds_77b1...
/// The first time such a token is charged, the simulator registers it as a Visa test card with
/// that outcome. Tokens of the older built-in simulator ("sim_{outcome}_{random}") are
/// recognised the same way, so cards saved before the simulator became a service keep working.
/// </summary>
public static partial class TestTokens
{
    private static readonly Dictionary<string, string> Last4ByOutcome = new()
    {
        [ChargeFailureCodes.Success] = "4242",
        [ChargeFailureCodes.CardDeclined] = "0002",
        [ChargeFailureCodes.InsufficientFunds] = "9995",
        [ChargeFailureCodes.ExpiredCard] = "0069",
        [ChargeFailureCodes.ProcessingError] = "0119",
    };

    public static CardToken? TryCreate(string? token, DateTime utcNow)
    {
        if (token is null)
            return null;

        var match = Pattern().Match(token);
        if (!match.Success || !Last4ByOutcome.TryGetValue(match.Groups["outcome"].Value, out var last4))
            return null;

        return new CardToken
        {
            Id = Guid.NewGuid(),
            Token = token,
            Last4Digits = last4,
            Brand = "Visa",
            ExpiryMonth = 12,
            ExpiryYear = utcNow.Year + 3,
            Outcome = match.Groups["outcome"].Value,
            CreatedAt = utcNow
        };
    }

    [GeneratedRegex("^(?:tok_test|sim)_(?<outcome>ok|card_declined|insufficient_funds|expired_card|processing_error)_[0-9a-f]{8,32}$")]
    private static partial Regex Pattern();
}
