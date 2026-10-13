namespace FairShare.Domain.Services;

/// <summary>
/// Rules for payment card numbers that do not depend on any payment provider:
/// the Luhn check digit (ISO/IEC 7812) and the card brand from the first digits.
/// </summary>
public static class CardNumber
{
    /// <summary>"4242 4242-4242 4242" -> "4242424242424242"</summary>
    public static string Normalize(string? value)
        => new((value ?? string.Empty).Where(char.IsDigit).ToArray());

    /// <summary>
    /// Luhn algorithm: from the right, every second digit is doubled (minus 9 if above 9);
    /// the sum of all digits must be divisible by 10. Catches almost every typo.
    /// </summary>
    public static bool IsValid(string? value)
    {
        var digits = Normalize(value);
        if (digits.Length is < 12 or > 19)
            return false;

        var sum = 0;
        var doubleIt = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var digit = digits[i] - '0';
            if (doubleIt)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
            doubleIt = !doubleIt;
        }
        return sum % 10 == 0;
    }

    public static string DetectBrand(string? value)
    {
        var digits = Normalize(value);
        if (digits.StartsWith('4')) return "Visa";
        if (digits.Length >= 2 && int.TryParse(digits[..2], out var two))
        {
            if (two is >= 51 and <= 55) return "Mastercard";
            if (two is 34 or 37) return "American Express";
        }
        if (digits.Length >= 4 && int.TryParse(digits[..4], out var four) && four is >= 2221 and <= 2720)
            return "Mastercard";
        return "Card";
    }
}
