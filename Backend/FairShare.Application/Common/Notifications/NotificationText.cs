using System.Globalization;
using FairShare.Domain.Entities;

namespace FairShare.Application.Common.Notifications;

/// <summary>
/// Small helpers so every notification is written the same way: Serbian Cyrillic, amounts as
/// "1.234,50 КМ", names in „quotes“, dates as "14.10.2026." The web app shows the text in
/// Latin script when the user chooses it, so only the Cyrillic version is stored.
/// </summary>
public static class NotificationText
{
    private static readonly NumberFormatInfo AmountFormat = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NumberGroupSizes = new[] { 3 }
    };

    private static readonly string[] Months =
    {
        "јануар", "фебруар", "март", "април", "мај", "јун",
        "јул", "август", "септембар", "октобар", "новембар", "децембар"
    };

    /// <summary>12.5m, "BAM" -> "12,50 КМ"</summary>
    public static string Money(decimal amount, string? currency)
        => $"{amount.ToString("#,##0.00", AmountFormat)} {CurrencySymbol(currency)}";

    /// <summary>„Стан Пајсијева 7“</summary>
    public static string Quote(string text) => $"„{text}“";

    public static string FullName(User user) => $"{user.FirstName} {user.LastName}";

    /// <summary>"14.10.2026."</summary>
    public static string Date(DateTime date) => date.ToString("dd.MM.yyyy.", CultureInfo.InvariantCulture);

    /// <summary>"2026-10" or a date -> "октобар 2026."</summary>
    public static string Month(DateTime date) => $"{Months[date.Month - 1]} {date.Year}.";

    private static string CurrencySymbol(string? currency) => currency?.ToUpperInvariant() switch
    {
        "BAM" => "КМ",
        "EUR" => "€",
        "RSD" => "дин.",
        "USD" => "$",
        null or "" => string.Empty,
        var code => code
    };
}
