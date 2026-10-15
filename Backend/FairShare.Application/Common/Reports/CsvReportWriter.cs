using System.Globalization;
using System.Text;

namespace FairShare.Application.Common.Reports;

/// <summary>
/// Writes reports as CSV for Excel: UTF-8 with BOM (so Excel shows Cyrillic and č, ć, đ
/// correctly), ";" as separator in Serbian, ISO dates and plain numbers. A report has several
/// tables, so the file is divided into sections: a title line, a header and the rows.
/// </summary>
public static class CsvReportWriter
{
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

    public static byte[] WriteSpending(SpendingReport report, ReportText text)
    {
        var a = report.Analytics;
        var csv = new Csv(text);

        csv.Section(text["spendingTitle"]);
        csv.Row(text["user"], csv.Text(report.UserName));
        csv.Row(text["from"], ReportText.CsvDate(a.From));
        csv.Row(text["to"], ReportText.CsvDate(a.To));
        csv.Row(text["currency"], a.Currency);
        csv.Row(text["totalSpent"], text.CsvNumber(a.TotalSpent));
        csv.Row(text["expenseCount"], a.ExpenseCount.ToString(CultureInfo.InvariantCulture));
        csv.Row(text["previousPeriod"], text.CsvNumber(a.PreviousPeriodTotal));
        csv.Row(text["change"] + " (%)", a.ChangePercentage is { } change ? text.CsvNumber(change) : text["noData"]);
        csv.Row(text["generated"], text.Moment(report.GeneratedAtUtc));
        csv.Blank();

        csv.Section(text["byCategory"]);
        csv.Row(text["category"], text["amount"], text["share"] + " (%)", text["countShort"], text["previousShort"]);
        foreach (var category in a.ByCategory)
        {
            csv.Row(
                csv.Text(category.CategoryName),
                text.CsvNumber(category.Total),
                text.CsvNumber(category.Percentage),
                category.Count.ToString(CultureInfo.InvariantCulture),
                text.CsvNumber(category.PreviousPeriodTotal));
        }
        csv.Blank();

        csv.Section(text["timeline"]);
        csv.Row(text["from"], text["to"], text["amount"]);
        foreach (var point in a.Timeline)
            csv.Row(ReportText.CsvDate(point.PeriodStart), ReportText.CsvDate(point.PeriodEnd), text.CsvNumber(point.Total));
        csv.Blank();

        csv.Section(text["expenses"]);
        csv.Row(
            text["date"], text["category"], text["description"], text["amount"], text["currency"],
            text.Format("amountIn", a.Currency), text["recurring"]);
        foreach (var expense in report.Expenses)
        {
            csv.Row(
                ReportText.CsvDate(expense.Date),
                csv.Text(expense.Category),
                csv.Text(expense.Description),
                text.CsvNumber(expense.Amount),
                expense.Currency,
                expense.ConvertedAmount is { } converted ? text.CsvNumber(converted) : string.Empty,
                expense.IsRecurring ? text["yes"] : text["no"]);
        }

        if (report.TotalExpenseCount > report.Expenses.Count)
        {
            csv.Blank();
            csv.Row(csv.Text(text.Format("truncatedNote", report.Expenses.Count, report.TotalExpenseCount)));
        }

        return csv.ToBytes();
    }

    public static byte[] WriteGroupDebts(GroupDebtReport report, ReportText text)
    {
        var csv = new Csv(text);

        csv.Section(text["groupTitle"]);
        csv.Row(text["group"], csv.Text(report.GroupName));
        csv.Row(text["currency"], report.Currency);
        csv.Row(text["groupTotal"], text.CsvNumber(report.TotalSpent));
        csv.Row(text["user"], csv.Text(report.RequestedBy));
        csv.Row(text["generated"], text.Moment(report.GeneratedAtUtc));
        csv.Blank();

        csv.Section(text["balances"]);
        csv.Row(text["member"], text["balance"], text["status"]);
        foreach (var balance in report.Balances)
            csv.Row(csv.Text(balance.Name), text.CsvNumber(balance.Balance), text.BalanceStatus(balance.Balance));
        csv.Blank();

        csv.Section(text["transfers"]);
        if (report.SuggestionUnavailable)
        {
            csv.Row(csv.Text(text["suggestionUnavailable"]));
        }
        else
        {
            csv.Row(text["payer"], text["recipient"], text["amount"]);
            foreach (var transfer in report.SuggestedTransfers)
                csv.Row(csv.Text(transfer.Debtor), csv.Text(transfer.Creditor), text.CsvNumber(transfer.Amount));
        }
        csv.Blank();

        csv.Section(text["completed"]);
        csv.Row(text["date"], text["payer"], text["recipient"], text["amount"]);
        foreach (var settlement in report.CompletedSettlements)
        {
            csv.Row(
                settlement.Date is { } date ? ReportText.CsvDate(date) : string.Empty,
                csv.Text(settlement.Debtor),
                csv.Text(settlement.Creditor),
                text.CsvNumber(settlement.Amount));
        }
        csv.Blank();

        csv.Section(text["groupExpenses"]);
        csv.Row(text["date"], text["description"], text["category"], text["paidBy"], text["amount"], text["split"], text["shares"]);
        foreach (var expense in report.Expenses)
        {
            var shares = string.Join(", ", expense.Shares.Select(s => $"{s.Name}: {text.CsvNumber(s.Amount)}"));
            csv.Row(
                ReportText.CsvDate(expense.Date),
                csv.Text(expense.Description),
                csv.Text(expense.Category),
                csv.Text(expense.PaidBy),
                text.CsvNumber(expense.Amount),
                text.SplitTypeName(expense.SplitType),
                csv.Text(shares));
        }

        if (report.TotalExpenseCount > report.Expenses.Count)
        {
            csv.Blank();
            csv.Row(csv.Text(text.Format("truncatedNote", report.Expenses.Count, report.TotalExpenseCount)));
        }

        return csv.ToBytes();
    }

    /// <summary>Builds the file line by line, quoting values where CSV requires it.</summary>
    private sealed class Csv(ReportText text)
    {
        private readonly ReportText _text = text;
        private readonly StringBuilder _builder = new();
        private readonly string _separator = text.CsvSeparator.ToString();

        public void Section(string title) => Row(title);

        public void Blank() => _builder.Append("\r\n");

        public void Row(params string[] values)
        {
            _builder.Append(string.Join(_separator, values.Select(Escape)));
            _builder.Append("\r\n");
        }

        /// <summary>
        /// Text typed by users. Shown in the report's script and protected against "CSV
        /// injection": a value starting with =, +, - or @ would be run by Excel as a formula,
        /// so it gets a leading apostrophe and stays plain text.
        /// </summary>
        public string Text(string? value)
        {
            var result = _text.User(value);
            return result.Length > 0 && "=+-@\t\r".Contains(result[0]) ? "'" + result : result;
        }

        public byte[] ToBytes() => Utf8WithBom.GetPreamble().Concat(Utf8WithBom.GetBytes(_builder.ToString())).ToArray();

        private string Escape(string value)
        {
            if (value.Contains(_separator) || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}
