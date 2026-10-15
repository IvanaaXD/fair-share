using System.Globalization;
using FairShare.Application.Common.Text;
using FairShare.Application.DTOs.Analytics;
using FairShare.Application.DTOs.Reports;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Services;

namespace FairShare.Application.Common.Reports;

/// <summary>
/// Labels and number/date formatting of reports in the user's language. Serbian Latin is made
/// from the Cyrillic labels by transliteration, exactly like in the web app.
///
/// Formatting does not depend on the server's culture data (the Docker image may run in
/// invariant globalization mode), so the separators are set here by hand.
/// </summary>
public sealed class ReportText
{
    private static readonly Dictionary<string, string> Cyrillic = new()
    {
        ["spendingTitle"] = "Извјештај о личној потрошњи",
        ["groupTitle"] = "Преглед дуговања у групи",
        ["period"] = "Период",
        ["user"] = "Корисник",
        ["group"] = "Група",
        ["generated"] = "Направљено",
        ["currency"] = "Валута",
        ["allAmountsIn"] = "Сви износи су у валути {0}.",
        ["summary"] = "Сажетак",
        ["from"] = "Од",
        ["to"] = "До",
        ["totalSpent"] = "Укупно потрошено",
        ["expenseCount"] = "Број трошкова",
        ["previousPeriod"] = "Претходни период",
        ["change"] = "Промјена",
        ["noData"] = "нема података",
        ["timeline"] = "Потрошња кроз вријеме",
        ["highest"] = "највише {0}",
        ["periodStart"] = "Почетак периода",
        ["periodEnd"] = "Крај периода",
        ["byCategory"] = "Потрошња по категоријама",
        ["category"] = "Категорија",
        ["share"] = "Удио",
        ["amount"] = "Износ",
        ["countShort"] = "Бр.",
        ["previousShort"] = "Прет. период",
        ["expenses"] = "Трошкови",
        ["date"] = "Датум",
        ["description"] = "Опис",
        ["amountIn"] = "Износ у {0}",
        ["recurring"] = "Понављајући",
        ["recurringMark"] = "понавља се",
        ["yes"] = "да",
        ["no"] = "не",
        ["noExpenses"] = "У овом периоду нема трошкова.",
        ["convertedNote"] = "Трошкови у другој валути (укупно {0}) претворени су по курсу од {1}. Извор курса: ExchangeRate-API.",
        ["convertedFixedNote"] = "Трошкови у другој валути (укупно {0}) претворени су по фиксном курсу 1 EUR = 1,95583 КМ.",
        ["unconvertedNote"] = "Није урачунато трошкова: {0} – курс њихове валуте тренутно није доступан.",
        ["truncatedNote"] = "Приказано је првих {0} трошкова; укупан број трошкова: {1}. Сви су урачунати у збирове.",
        ["page"] = "Страна",
        ["of"] = "од",
        ["balances"] = "Стање чланова",
        ["member"] = "Члан",
        ["balance"] = "Салдо",
        ["status"] = "Стање",
        ["owes"] = "дугује",
        ["isOwed"] = "потражује",
        ["settled"] = "измирено",
        ["you"] = "ти",
        ["balanceHint"] = "Позитиван салдо значи да група дугује члану, а негативан да члан дугује групи.",
        ["outstanding"] = "Укупно за измирење",
        ["paymentsNeeded"] = "Потребних уплата",
        ["members"] = "Чланови",
        ["transfers"] = "Како измирити дугове",
        ["transfersHint"] = "Најмањи број уплата којима се измирују сва дуговања, израчунат у тренутку прављења извјештаја.",
        ["payer"] = "Ко уплаћује",
        ["recipient"] = "Коме",
        ["allSettled"] = "Сва дуговања су измирена.",
        ["suggestionUnavailable"] = "Приједлог није могуће израчунати јер постоји неизмирен салдо бившег члана.",
        ["completed"] = "Измирена поравнања",
        ["noCompleted"] = "Још нема измирених поравнања.",
        ["groupExpenses"] = "Трошкови групе",
        ["groupTotal"] = "Укупно потрошено у групи",
        ["paidBy"] = "Платилац",
        ["split"] = "Подјела",
        ["splitEqual"] = "једнако",
        ["splitPercentage"] = "процентуално",
        ["splitExact"] = "тачни износи",
        ["shares"] = "Удјели",
        ["noGroupExpenses"] = "Група још нема трошкова.",
        ["at"] = "у",
    };

    private static readonly Dictionary<string, string> English = new()
    {
        ["spendingTitle"] = "Personal spending report",
        ["groupTitle"] = "Group debt overview",
        ["period"] = "Period",
        ["user"] = "User",
        ["group"] = "Group",
        ["generated"] = "Generated",
        ["currency"] = "Currency",
        ["allAmountsIn"] = "All amounts are in {0}.",
        ["summary"] = "Summary",
        ["from"] = "From",
        ["to"] = "To",
        ["totalSpent"] = "Total spent",
        ["expenseCount"] = "Expenses",
        ["previousPeriod"] = "Previous period",
        ["change"] = "Change",
        ["noData"] = "no data",
        ["timeline"] = "Spending over time",
        ["highest"] = "highest {0}",
        ["periodStart"] = "Period start",
        ["periodEnd"] = "Period end",
        ["byCategory"] = "Spending by category",
        ["category"] = "Category",
        ["share"] = "Share",
        ["amount"] = "Amount",
        ["countShort"] = "No.",
        ["previousShort"] = "Prev. period",
        ["expenses"] = "Expenses",
        ["date"] = "Date",
        ["description"] = "Description",
        ["amountIn"] = "Amount in {0}",
        ["recurring"] = "Recurring",
        ["recurringMark"] = "recurring",
        ["yes"] = "yes",
        ["no"] = "no",
        ["noExpenses"] = "There are no expenses in this period.",
        ["convertedNote"] = "Expenses in another currency ({0} in total) were converted at the rates of {1}. Rates by ExchangeRate-API.",
        ["convertedFixedNote"] = "Expenses in another currency ({0} in total) were converted at the fixed rate 1 EUR = 1.95583 BAM.",
        ["unconvertedNote"] = "Expenses not included: {0} – the exchange rate of their currency is not available right now.",
        ["truncatedNote"] = "The first {0} expenses are listed; expenses in total: {1}. All of them are included in the totals.",
        ["page"] = "Page",
        ["of"] = "of",
        ["balances"] = "Member balances",
        ["member"] = "Member",
        ["balance"] = "Balance",
        ["status"] = "Status",
        ["owes"] = "owes",
        ["isOwed"] = "is owed",
        ["settled"] = "settled",
        ["you"] = "you",
        ["balanceHint"] = "A positive balance means the group owes the member; a negative one means the member owes the group.",
        ["outstanding"] = "Total outstanding",
        ["paymentsNeeded"] = "Payments needed",
        ["members"] = "Members",
        ["transfers"] = "How to settle up",
        ["transfersHint"] = "The fewest payments that settle all debts, calculated when this report was made.",
        ["payer"] = "Who pays",
        ["recipient"] = "To whom",
        ["allSettled"] = "All debts are settled.",
        ["suggestionUnavailable"] = "A suggestion cannot be calculated because a former member still has an unsettled balance.",
        ["completed"] = "Completed settlements",
        ["noCompleted"] = "No settlements have been completed yet.",
        ["groupExpenses"] = "Group expenses",
        ["groupTotal"] = "Total group spending",
        ["paidBy"] = "Paid by",
        ["split"] = "Split",
        ["splitEqual"] = "equally",
        ["splitPercentage"] = "by percentage",
        ["splitExact"] = "exact amounts",
        ["shares"] = "Shares",
        ["noGroupExpenses"] = "The group has no expenses yet.",
        ["at"] = "at",
    };

    private static readonly Dictionary<string, string> Latin =
        Cyrillic.ToDictionary(pair => pair.Key, pair => CyrillicTransliterator.ToLatin(pair.Value));

    private static readonly string[] CyrillicMonths =
        ["јан", "феб", "мар", "апр", "мај", "јун", "јул", "авг", "сеп", "окт", "нов", "дец"];

    private readonly Dictionary<string, string> _labels;
    private readonly NumberFormatInfo _numbers;
    private readonly TimeZoneInfo _timeZone;

    private ReportText(ReportLanguage language, TimeZoneInfo timeZone)
    {
        Language = language;
        _timeZone = timeZone;
        _labels = language switch
        {
            ReportLanguage.En => English,
            ReportLanguage.SrLatn => Latin,
            _ => Cyrillic
        };

        _numbers = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        if (!IsEnglish)
        {
            _numbers.NumberDecimalSeparator = ",";
            _numbers.NumberGroupSeparator = ".";
        }
    }

    /// <param name="timeZoneId">IANA id from the browser (e.g. "Europe/Sarajevo"); UTC if unknown.</param>
    public static ReportText For(ReportLanguage language, string? timeZoneId)
        => new(language, FindTimeZone(timeZoneId));

    public ReportLanguage Language { get; }
    public bool IsEnglish => Language == ReportLanguage.En;

    /// <summary>Value of the HTML/PDF "lang" attribute.</summary>
    public string LanguageTag => Language switch
    {
        ReportLanguage.En => "en",
        ReportLanguage.SrLatn => "sr-Latn",
        _ => "sr-Cyrl"
    };

    public string this[string key] => _labels.TryGetValue(key, out var value) ? value : key;

    public string Format(string key, params object[] args)
        => string.Format(CultureInfo.InvariantCulture, this[key], args);

    /// <summary>
    /// Text entered by users (names, categories, descriptions). Cyrillic is shown in Latin in
    /// every language except Serbian Cyrillic - the same rule as in the web app.
    /// </summary>
    public string User(string? text)
        => Language == ReportLanguage.SrCyrl ? text ?? string.Empty : CyrillicTransliterator.ToLatin(text);

    // ---------- numbers ----------

    public string Amount(decimal value) => value.ToString("#,##0.00", _numbers);

    public string Money(decimal value, string currency) => $"{Amount(value)} {CurrencyLabel(currency)}";

    public string CurrencyLabel(string currency)
    {
        var code = currency.Trim().ToUpperInvariant();
        if (code == "BAM" && !IsEnglish)
            return Language == ReportLanguage.SrCyrl ? "КМ" : "KM";
        return code;
    }

    public string Percent(decimal value)
        => IsEnglish ? $"{value.ToString("0.#", _numbers)}%" : $"{value.ToString("0.#", _numbers)} %";

    /// <summary>"+12,5 %" or "−3 %" (a real minus sign, like in the app).</summary>
    public string SignedPercent(decimal value)
    {
        var sign = value > 0 ? "+" : value < 0 ? "−" : string.Empty;
        return sign + Percent(Math.Abs(value));
    }

    // ---------- dates ----------

    /// <summary>Calendar date of an expense (stored as midnight UTC, so it is not shifted).</summary>
    public string Date(DateTime value)
        => IsEnglish
            ? value.ToString("d MMM yyyy", CultureInfo.InvariantCulture)
            : value.ToString("dd.MM.yyyy.", CultureInfo.InvariantCulture);

    public string DateRange(DateTime from, DateTime to) => $"{Date(from)} – {Date(to)}";

    /// <summary>A moment in time (e.g. when the report was made), in the user's time zone.</summary>
    public string Moment(DateTime utc)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _timeZone);
        var time = local.ToString("HH:mm", CultureInfo.InvariantCulture);
        return IsEnglish ? $"{Date(local)}, {time}" : $"{Date(local)} {this["at"]} {time}";
    }

    /// <summary>Short label of a chart column: a day, the first day of a week, or a month.</summary>
    public string BucketLabel(DateTime start, TimeGranularity granularity)
    {
        if (granularity != TimeGranularity.Month)
            return IsEnglish
                ? start.ToString("d MMM", CultureInfo.InvariantCulture)
                : start.ToString("dd.MM.", CultureInfo.InvariantCulture);

        if (IsEnglish)
            return start.ToString("MMM yyyy", CultureInfo.InvariantCulture);

        var month = CyrillicMonths[start.Month - 1];
        if (Language == ReportLanguage.SrLatn)
            month = CyrillicTransliterator.ToLatin(month);
        return $"{month} {start.Year}.";
    }

    public string SplitTypeName(SplitType splitType) => splitType switch
    {
        SplitType.Percentage => this["splitPercentage"],
        SplitType.ExactAmount => this["splitExact"],
        _ => this["splitEqual"]
    };

    /// <summary>"owes", "is owed" or "settled" for a member's net balance in a group.</summary>
    public string BalanceStatus(decimal balance)
        => balance > GroupBalanceCalculator.Tolerance ? this["isOwed"]
            : balance < -GroupBalanceCalculator.Tolerance ? this["owes"]
            : this["settled"];

    // ---------- CSV ----------

    /// <summary>Excel in our region expects ";" (the comma is the decimal separator there).</summary>
    public char CsvSeparator => IsEnglish ? ',' : ';';

    /// <summary>Numbers in a CSV have no thousands separator, so Excel reads them as numbers.</summary>
    public string CsvNumber(decimal value) => value.ToString("0.00", _numbers);

    /// <summary>ISO dates are recognised by Excel in every language.</summary>
    public static string CsvDate(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // ---------- helpers ----------

    private static TimeZoneInfo FindTimeZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 64)
            return TimeZoneInfo.Utc;

        try
        {
            // .NET understands IANA ids on Windows too (through ICU).
            return TimeZoneInfo.FindSystemTimeZoneById(id.Trim());
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
