using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FairShare.Domain.Services;

/// <summary>Подаци садржани у QR коду за уплату.</summary>
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
/// Генерисање и парсирање текста QR кода за уплату (функционалност 5.8), по узору на
/// IPS QR стандард Народне банке Србије: поља облика ОЗНАКА:вриједност раздвојена знаком "|".
///
/// Поједностављења у односу на прави стандард (наводе се у раду):
/// - рачун се прихвата са 16 цифара (BiH) или 18 цифара (Србија), без контроле контролног броја;
/// - позив на број (RO) је интерни идентификатор поравнања, без модела и контролних цифара.
/// </summary>
public static class IpsQrCodec
{
    public const int MaxNameLength = 70;
    public const int MaxPurposeLength = 35;
    public const int MaxReferenceLength = 35;

    private static readonly Regex AccountRegex = new(@"^\d{16}$|^\d{18}$", RegexOptions.Compiled);
    private static readonly Regex AmountRegex = new(@"^([A-Z]{3})(\d{1,15})(,\d{1,2})?$", RegexOptions.Compiled);
    private static readonly Regex PaymentCodeRegex = new(@"^\d{3}$", RegexOptions.Compiled);

    /// <summary>Уклања размаке и цртице из броја рачуна ("161-0000012345678-90" -> "161000001234567890").</summary>
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
            return Fail("QR код је празан.");

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in text.Trim().Split('|'))
        {
            var separator = part.IndexOf(':');
            if (separator <= 0)
            {
                errors.Add($"Неисправно поље '{part}' (очекиван облик ОЗНАКА:вриједност).");
                continue;
            }

            var tag = part[..separator].Trim().ToUpperInvariant();
            var value = part[(separator + 1)..].Trim();

            if (!fields.TryAdd(tag, value))
                errors.Add($"Поље '{tag}' се појављује више пута.");
        }

        RequireEquals(fields, "K", "PR", errors);
        RequireEquals(fields, "V", "01", errors);
        RequireEquals(fields, "C", "1", errors);

        var account = fields.GetValueOrDefault("R");
        if (!IsValidAccount(account))
            errors.Add("Рачун примаоца (R) мора имати 16 или 18 цифара.");

        var name = fields.GetValueOrDefault("N");
        if (string.IsNullOrWhiteSpace(name))
            errors.Add("Назив примаоца (N) је обавезан.");
        else if (name.Length > MaxNameLength)
            errors.Add($"Назив примаоца (N) може имати највише {MaxNameLength} карактера.");

        string currency = string.Empty;
        decimal amount = 0;
        var amountField = fields.GetValueOrDefault("I");
        var amountMatch = amountField is null ? null : AmountRegex.Match(amountField);
        if (amountMatch is null || !amountMatch.Success)
        {
            errors.Add("Износ (I) мора бити у облику ВАЛУТАизнос, нпр. BAM12,50.");
        }
        else
        {
            currency = amountMatch.Groups[1].Value;
            var number = amountMatch.Groups[2].Value + amountMatch.Groups[3].Value.Replace(',', '.');
            amount = decimal.Parse(number, CultureInfo.InvariantCulture);
            if (amount <= 0)
                errors.Add("Износ (I) мора бити већи од нуле.");
        }

        var paymentCode = fields.GetValueOrDefault("SF");
        if (paymentCode is not null && !PaymentCodeRegex.IsMatch(paymentCode))
            errors.Add("Шифра плаћања (SF) мора имати тачно 3 цифре.");

        var purpose = fields.GetValueOrDefault("S");
        if (purpose is not null && purpose.Length > MaxPurposeLength)
            errors.Add($"Сврха плаћања (S) може имати највише {MaxPurposeLength} карактера.");

        var reference = fields.GetValueOrDefault("RO");
        if (reference is not null && reference.Length > MaxReferenceLength)
            errors.Add($"Позив на број (RO) може имати највише {MaxReferenceLength} карактера.");

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
            errors.Add($"Поље {tag} мора имати вриједност '{expected}'.");
    }

    /// <summary>Износ са два децимала и запетом: 12.5 -> "12,50".</summary>
    private static string FormatAmount(decimal amount)
        => amount.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');

    /// <summary>Уклања знак "|" (резервисан као раздвајач) и скраћује на дозвољену дужину.</summary>
    private static string Clean(string value, int maxLength)
    {
        var cleaned = new StringBuilder(value.Trim()).Replace("|", " ").ToString();
        return cleaned.Length <= maxLength ? cleaned : cleaned[..maxLength];
    }
}
