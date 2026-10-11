using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FairShare.Domain.Services;

/// <summary>Data contained in the payment QR code.</summary>
public record IpsQrPayload(
    string RecipientAccount,
    string RecipientName,
    string Currency,
    decimal Amount,
    string? PaymentCode,
    string? Purpose,
    string? Reference);

public record IpsQrParseResult(bool IsValid, IpsQrPayload? Payload, IReadOnlyList<string> Errors);

/// <summary>
/// Generation and parsing of the payment QR code text (feature 5.8), modeled after the
/// IPS QR standard of the National Bank of Serbia: fields in the format TAG:value separated by the "|" character.
///
/// Simplifications compared to the real standard (mentioned in the thesis):
/// - account is accepted with 16 digits (BiH) or 18 digits (Serbia), without checksum validation;
/// - reference number (RO) is an internal settlement identifier, without model and control digits.
/// </summary>
public static class IpsQrCodec
{
    public const int MaxNameLength = 70;
    public const int MaxPurposeLength = 35;
    public const int MaxReferenceLength = 35;

    private static readonly Regex AccountRegex = new(@"^\d{16}$|^\d{18}$", RegexOptions.Compiled);
    private static readonly Regex AmountRegex = new(@"^([A-Z]{3})(\d{1,15})(,\d{1,2})?$", RegexOptions.Compiled);
    private static readonly Regex PaymentCodeRegex = new(@"^\d{3}$", RegexOptions.Compiled);

    /// <summary>Removes spaces and dashes from the account number ("161-0000012345678-90" -> "161000001234567890").</summary>
    public static string NormalizeAccount(string account)
        => new(account.Where(char.IsDigit).ToArray());

    public static bool IsValidAccount(string? account)
        => account is not null && AccountRegex.IsMatch(account);

    public static string Build(IpsQrPayload payload)
    {
        var parts = new List<string>
        {
            "K:PR",
            "V:01",
            "C:1",
            $"R:{NormalizeAccount(payload.RecipientAccount)}",
            $"N:{Clean(payload.RecipientName, MaxNameLength)}",
            $"I:{payload.Currency.ToUpperInvariant()}{FormatAmount(payload.Amount)}"
        };

        if (!string.IsNullOrWhiteSpace(payload.PaymentCode))
            parts.Add($"SF:{payload.PaymentCode}");
        if (!string.IsNullOrWhiteSpace(payload.Purpose))
            parts.Add($"S:{Clean(payload.Purpose, MaxPurposeLength)}");
        if (!string.IsNullOrWhiteSpace(payload.Reference))
            parts.Add($"RO:{Clean(payload.Reference, MaxReferenceLength)}");

        return string.Join("|", parts);
    }

    public static IpsQrParseResult Parse(string? text)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(text))
            return Fail("QR code is empty.");

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in text.Trim().Split('|'))
        {
            var separator = part.IndexOf(':');
            if (separator <= 0)
            {
                errors.Add($"Invalid field '{part}' (expected format TAG:value).");
                continue;
            }

            var tag = part[..separator].Trim().ToUpperInvariant();
            var value = part[(separator + 1)..].Trim();

            if (!fields.TryAdd(tag, value))
                errors.Add($"Field '{tag}' appears multiple times.");
        }

        RequireEquals(fields, "K", "PR", errors);
        RequireEquals(fields, "V", "01", errors);
        RequireEquals(fields, "C", "1", errors);

        var account = fields.GetValueOrDefault("R");
        if (!IsValidAccount(account))
            errors.Add("Recipient account (R) must have 16 or 18 digits.");

        var name = fields.GetValueOrDefault("N");
        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Recipient name (N) is required.");
        else if (name.Length > MaxNameLength)
            errors.Add($"Recipient name (N) can have at most {MaxNameLength} characters.");

        string currency = string.Empty;
        decimal amount = 0;
        var amountField = fields.GetValueOrDefault("I");
        var amountMatch = amountField is null ? null : AmountRegex.Match(amountField);
        if (amountMatch is null || !amountMatch.Success)
        {
            errors.Add("Amount (I) must be in the format CURRENCYamount, e.g., BAM12,50.");
        }
        else
        {
            currency = amountMatch.Groups[1].Value;
            var number = amountMatch.Groups[2].Value + amountMatch.Groups[3].Value.Replace(',', '.');
            amount = decimal.Parse(number, CultureInfo.InvariantCulture);
            if (amount <= 0)
                errors.Add("Amount (I) must be greater than zero.");
        }

        var paymentCode = fields.GetValueOrDefault("SF");
        if (paymentCode is not null && !PaymentCodeRegex.IsMatch(paymentCode))
            errors.Add("Payment code (SF) must have exactly 3 digits.");

        var purpose = fields.GetValueOrDefault("S");
        if (purpose is not null && purpose.Length > MaxPurposeLength)
            errors.Add($"Payment purpose (S) can have at most {MaxPurposeLength} characters.");

        var reference = fields.GetValueOrDefault("RO");
        if (reference is not null && reference.Length > MaxReferenceLength)
            errors.Add($"Reference number (RO) can have at most {MaxReferenceLength} characters.");

        if (errors.Count > 0)
            return new IpsQrParseResult(false, null, errors);

        return new IpsQrParseResult(
            true,
            new IpsQrPayload(account!, name!, currency, amount, paymentCode, purpose, reference),
            errors);

        static IpsQrParseResult Fail(string error) => new(false, null, new[] { error });
    }

    private static void RequireEquals(Dictionary<string, string> fields, string tag, string expected, List<string> errors)
    {
        if (!fields.TryGetValue(tag, out var value) || value != expected)
            errors.Add($"Field {tag} must have the value '{expected}'.");
    }

    /// <summary>Amount with two decimal places and a comma: 12.5 -> "12,50".</summary>
    private static string FormatAmount(decimal amount)
        => amount.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');

    /// <summary>Removes the "|" character (reserved as a separator) and truncates to the allowed length.</summary>
    private static string Clean(string value, int maxLength)
    {
        var cleaned = new StringBuilder(value.Trim()).Replace("|", " ").ToString();
        return cleaned.Length <= maxLength ? cleaned : cleaned[..maxLength];
    }
}