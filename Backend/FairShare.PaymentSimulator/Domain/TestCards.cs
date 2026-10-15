namespace FairShare.PaymentSimulator.Domain;

/// <summary>
/// Card rules of the simulator. Only test numbers are meant to be used; like real providers'
/// sandboxes, a few well-known numbers give the declined outcomes:
///   4000 0000 0000 0002  -> card_declined
///   4000 0000 0000 9995  -> insufficient_funds
///   4000 0000 0000 0069  -> expired_card
///   4000 0000 0000 0119  -> processing_error
/// every other valid number (e.g. 4242 4242 4242 4242) is charged successfully.
/// </summary>
public static class TestCards
{
    private static readonly Dictionary<string, string> Outcomes = new()
    {
        ["4000000000000002"] = ChargeFailureCodes.CardDeclined,
        ["4000000000009995"] = ChargeFailureCodes.InsufficientFunds,
        ["4000000000000069"] = ChargeFailureCodes.ExpiredCard,
        ["4000000000000119"] = ChargeFailureCodes.ProcessingError,
    };

    public static string Normalize(string? number)
        => new((number ?? string.Empty).Where(char.IsDigit).ToArray());

    public static string OutcomeOf(string normalizedNumber)
        => Outcomes.GetValueOrDefault(normalizedNumber, ChargeFailureCodes.Success);

    /// <summary>Luhn checksum - catches almost every typo in a card number.</summary>
    public static bool IsValidNumber(string normalized)
    {
        if (normalized.Length is < 12 or > 19)
            return false;

        var sum = 0;
        var doubleIt = false;
        for (var i = normalized.Length - 1; i >= 0; i--)
        {
            var digit = normalized[i] - '0';
            if (doubleIt)
            {
                digit *= 2;
                if (digit > 9)
                    digit -= 9;
            }

            sum += digit;
            doubleIt = !doubleIt;
        }

        return sum % 10 == 0;
    }

    public static string BrandOf(string normalized) => normalized switch
    {
        _ when normalized.StartsWith('4') => "Visa",
        _ when normalized.Length >= 2 && int.Parse(normalized[..2]) is >= 51 and <= 55 => "Mastercard",
        _ when normalized.Length >= 4 && int.Parse(normalized[..4]) is >= 2221 and <= 2720 => "Mastercard",
        _ when normalized.StartsWith("34") || normalized.StartsWith("37") => "American Express",
        _ when normalized.StartsWith("6") => "Discover",
        _ => "Card"
    };
}
