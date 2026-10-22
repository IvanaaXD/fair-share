using System.Globalization;
using FairShare.Application.Abstractions;
using FairShare.Application.Common.Reports;
using FairShare.Application.DTOs.Analytics;
using FairShare.Domain.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FairShare.Infrastructure.Reports;

/// <summary>
/// PDF reports with QuestPDF. The layout follows the web app: the same font, colors and
/// order of information (summary first, then details), on A4 with page numbers.
/// </summary>
public sealed class QuestPdfReportRenderer : IPdfReportRenderer
{
    private const float ChartHeight = 110;

    public QuestPdfReportRenderer() => QuestPdfSetup.Configure();

    // ======================================================================
    // Personal spending
    // ======================================================================

    public byte[] RenderSpending(SpendingReport report, ReportText text)
    {
        var a = report.Analytics;
        var title = text["spendingTitle"];

        var document = Document.Create(container => container.Page(page =>
        {
            SetUpPage(page, title, text, report.GeneratedAtUtc);

            page.Content().Column(column =>
            {
                Title(column, title,
                    $"{text.User(report.UserName)} · {text.DateRange(a.From, a.To)}",
                    text.Format("allAmountsIn", text.CurrencyLabel(a.Currency)));

                column.Item().PaddingTop(16).Row(row =>
                {
                    row.Spacing(10);
                    Kpi(row.RelativeItem(1.2f), text["totalSpent"], text.Money(a.TotalSpent, a.Currency), Palette.Primary);
                    Kpi(row.RelativeItem(0.8f), text["expenseCount"], a.ExpenseCount.ToString(CultureInfo.InvariantCulture));
                    Kpi(row.RelativeItem(1.2f), text["previousPeriod"], text.Money(a.PreviousPeriodTotal, a.Currency),
                        note: text.DateRange(a.PreviousFrom, a.PreviousTo));
                    Kpi(row.RelativeItem(0.8f), text["change"],
                        a.ChangePercentage is { } change ? text.SignedPercent(change) : text["noData"],
                        // More spending than before is shown in amber, less in petrol - like in the app.
                        a.ChangePercentage > 0 ? Palette.Negative : a.ChangePercentage < 0 ? Palette.Primary : (Color?)null);
                });

                if (a.ConvertedExpenseCount > 0)
                {
                    var note = a.RatesUpdatedAt is { } ratesDate
                        ? text.Format("convertedNote", a.ConvertedExpenseCount, text.Date(ratesDate))
                        : text.Format("convertedFixedNote", a.ConvertedExpenseCount);
                    column.Item().PaddingTop(8).Text(note).FontSize(8).FontColor(Palette.Muted);
                }

                if (a.UnconvertedExpenseCount > 0)
                {
                    column.Item().PaddingTop(4).Text(text.Format("unconvertedNote", a.UnconvertedExpenseCount))
                        .FontSize(8).FontColor(Palette.Negative);
                }

                if (a.ExpenseCount == 0 && report.Expenses.Count == 0)
                {
                    column.Item().PaddingTop(24).Element(Note).Text(text["noExpenses"]);
                    return;
                }

                Section(column, text["timeline"]);
                column.Item().Element(c => TimelineChart(c, a, text));

                Section(column, text["byCategory"]);
                column.Item().Element(c => CategoryTable(c, a, text));

                Section(column, text["expenses"]);
                column.Item().Element(c => SpendingExpenseTable(c, report, text));

                if (report.TotalExpenseCount > report.Expenses.Count)
                {
                    column.Item().PaddingTop(6)
                        .Text(text.Format("truncatedNote", report.Expenses.Count, report.TotalExpenseCount))
                        .FontSize(8).FontColor(Palette.Muted);
                }
            });
        }));

        return document.WithMetadata(Metadata(title, text)).GeneratePdf();
    }

    private static void TimelineChart(IContainer container, SpendingAnalyticsResponse a, ReportText text)
    {
        var points = a.Timeline;
        var max = points.Count == 0 ? 0m : points.Max(p => p.Total);
        var spacing = points.Count > 20 ? 2f : 4f;

        container.Column(column =>
        {
            column.Item().Text(text.Format("highest", text.Money(max, a.Currency))).FontSize(7.5f).FontColor(Palette.Muted);

            // Columns of the chart: height proportional to the total of the day/week/month.
            column.Item().PaddingTop(6).Height(ChartHeight).Row(row =>
            {
                row.Spacing(spacing);
                foreach (var point in points)
                {
                    var item = row.RelativeItem().AlignBottom();
                    if (point.Total <= 0 || max <= 0)
                        continue;

                    // Never thinner than 1.5 pt, so a small amount is still visible.
                    var height = Math.Max(1.5f, (float)(point.Total / max) * (ChartHeight - 2));
                    item.Height(height).Background(Palette.Primary);
                }
            });
            column.Item().LineHorizontal(0.75f).LineColor(Palette.Border);

            // Labels under the columns when there is room for them; otherwise only both ends.
            if (points.Count is > 0 and <= 14)
            {
                column.Item().PaddingTop(3).Row(row =>
                {
                    row.Spacing(spacing);
                    foreach (var point in points)
                    {
                        row.RelativeItem().AlignCenter()
                            .Text(text.BucketLabel(point.PeriodStart, a.Granularity)).FontSize(7).FontColor(Palette.Muted);
                    }
                });
            }
            else if (points.Count > 14)
            {
                column.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text(text.Date(points[0].PeriodStart)).FontSize(7).FontColor(Palette.Muted);
                    row.RelativeItem().AlignRight().Text(text.Date(points[^1].PeriodEnd)).FontSize(7).FontColor(Palette.Muted);
                });
            }
        });
    }

    private static void CategoryTable(IContainer container, SpendingAnalyticsResponse a, ReportText text)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(2.4f);
                columns.ConstantColumn(78);
                columns.ConstantColumn(30);
                columns.ConstantColumn(78);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text(text["category"]);
                header.Cell().Element(HeaderCell).Text(text["share"]);
                header.Cell().Element(HeaderCell).AlignRight().Text(text["amount"]);
                header.Cell().Element(HeaderCell).AlignRight().Text(text["countShort"]);
                header.Cell().Element(HeaderCell).AlignRight().Text(text["previousShort"]);
            });

            foreach (var category in a.ByCategory)
            {
                table.Cell().Element(BodyCell).Text(text.User(category.CategoryName));
                table.Cell().Element(BodyCell).Element(c => ShareBar(c, category.Percentage, text));
                table.Cell().Element(BodyCell).AlignRight().Text(text.Amount(category.Total)).Bold();
                table.Cell().Element(BodyCell).AlignRight().Text(category.Count.ToString(CultureInfo.InvariantCulture));
                table.Cell().Element(BodyCell).AlignRight().Text(text.Amount(category.PreviousPeriodTotal)).FontColor(Palette.Muted);
            }
        });
    }

    private static void SpendingExpenseTable(IContainer container, SpendingReport report, ReportText text)
    {
        var currency = report.Analytics.Currency;

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(60);
                columns.RelativeColumn(2);
                columns.RelativeColumn(3);
                columns.ConstantColumn(82);
                columns.ConstantColumn(72);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text(text["date"]);
                header.Cell().Element(HeaderCell).Text(text["category"]);
                header.Cell().Element(HeaderCell).Text(text["description"]);
                header.Cell().Element(HeaderCell).AlignRight().Text(text["amount"]);
                header.Cell().Element(HeaderCell).AlignRight().Text(text.Format("amountIn", text.CurrencyLabel(currency)));
            });

            foreach (var expense in report.Expenses)
            {
                table.Cell().Element(BodyCell).Text(text.Date(expense.Date));
                table.Cell().Element(BodyCell).Text(text.User(expense.Category));
                table.Cell().Element(BodyCell).Text(t =>
                {
                    t.Span(text.User(expense.Description));
                    if (expense.IsRecurring)
                    {
                        var separator = string.IsNullOrWhiteSpace(expense.Description) ? string.Empty : " · ";
                        t.Span(separator + text["recurringMark"]).FontSize(7.5f).FontColor(Palette.Muted);
                    }
                });
                table.Cell().Element(BodyCell).AlignRight().Text(text.Money(expense.Amount, expense.Currency));
                table.Cell().Element(BodyCell).AlignRight()
                    .Text(expense.ConvertedAmount is { } converted ? text.Amount(converted) : "–").Bold();
            }
        });
    }

    // ======================================================================
    // Group debts
    // ======================================================================

    public byte[] RenderGroupDebts(GroupDebtReport report, ReportText text)
    {
        var title = text["groupTitle"];
        var outstanding = report.Balances.Where(b => b.Balance > 0).Sum(b => b.Balance);

        var document = Document.Create(container => container.Page(page =>
        {
            SetUpPage(page, title, text, report.GeneratedAtUtc);

            page.Content().Column(column =>
            {
                Title(column, title,
                    text.User(report.GroupName),
                    text.Format("allAmountsIn", text.CurrencyLabel(report.Currency)));

                column.Item().PaddingTop(16).Row(row =>
                {
                    row.Spacing(10);
                    Kpi(row.RelativeItem(1.2f), text["groupTotal"], text.Money(report.TotalSpent, report.Currency), Palette.Primary,
                        note: $"{text["expenseCount"]}: {report.TotalExpenseCount}");
                    Kpi(row.RelativeItem(0.8f), text["members"], report.Balances.Count.ToString(CultureInfo.InvariantCulture));
                    Kpi(row.RelativeItem(1.2f), text["outstanding"], text.Money(outstanding, report.Currency),
                        outstanding > 0 ? Palette.Negative : (Color?)null);
                    Kpi(row.RelativeItem(0.8f), text["paymentsNeeded"],
                        report.SuggestionUnavailable ? "–" : report.SuggestedTransfers.Count.ToString(CultureInfo.InvariantCulture));
                });

                Section(column, text["balances"], text["balanceHint"]);
                column.Item().Element(c => BalanceTable(c, report, text));

                Section(column, text["transfers"], text["transfersHint"]);
                if (report.SuggestionUnavailable)
                    column.Item().Element(Note).Text(text["suggestionUnavailable"]).FontColor(Palette.Negative);
                else if (report.SuggestedTransfers.Count == 0)
                    column.Item().Element(Note).Text(text["allSettled"]);
                else
                    column.Item().Element(c => TransferTable(c, report.SuggestedTransfers, text, withDate: false));

                Section(column, text["completed"]);
                if (report.CompletedSettlements.Count == 0)
                    column.Item().Element(Note).Text(text["noCompleted"]);
                else
                    column.Item().Element(c => TransferTable(c, report.CompletedSettlements, text, withDate: true));

                Section(column, text["groupExpenses"]);
                if (report.Expenses.Count == 0)
                    column.Item().Element(Note).Text(text["noGroupExpenses"]);
                else
                    column.Item().Element(c => GroupExpenseTable(c, report, text));

                if (report.TotalExpenseCount > report.Expenses.Count)
                {
                    column.Item().PaddingTop(6)
                        .Text(text.Format("truncatedNote", report.Expenses.Count, report.TotalExpenseCount))
                        .FontSize(8).FontColor(Palette.Muted);
                }
            });
        }));

        return document.WithMetadata(Metadata(title, text)).GeneratePdf();
    }

    private static void BalanceTable(IContainer container, GroupDebtReport report, ReportText text)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.ConstantColumn(96);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text(text["member"]);
                header.Cell().Element(HeaderCell).Text(text["status"]);
                header.Cell().Element(HeaderCell).AlignRight().Text(text["balance"]);
            });

            foreach (var balance in report.Balances)
            {
                var color = BalanceColor(balance.Balance);

                table.Cell().Element(BodyCell).Text(t =>
                {
                    t.Span(text.User(balance.Name));
                    if (balance.IsCurrentUser)
                        t.Span($" ({text["you"]})").FontColor(Palette.Muted);
                });
                table.Cell().Element(BodyCell).Text(text.BalanceStatus(balance.Balance)).FontColor(color);
                table.Cell().Element(BodyCell).AlignRight().Text(SignedAmount(balance.Balance, text)).Bold().FontColor(color);
            }
        });
    }

    private static void TransferTable(
        IContainer container, IReadOnlyList<GroupReportTransfer> transfers, ReportText text, bool withDate)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                if (withDate)
                    columns.ConstantColumn(66);
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.ConstantColumn(96);
            });

            table.Header(header =>
            {
                if (withDate)
                    header.Cell().Element(HeaderCell).Text(text["date"]);
                header.Cell().Element(HeaderCell).Text(text["payer"]);
                header.Cell().Element(HeaderCell).Text(text["recipient"]);
                header.Cell().Element(HeaderCell).AlignRight().Text(text["amount"]);
            });

            foreach (var transfer in transfers)
            {
                if (withDate)
                    table.Cell().Element(BodyCell).Text(transfer.Date is { } date ? text.Date(date) : string.Empty);
                table.Cell().Element(BodyCell).Text(text.User(transfer.Debtor));
                table.Cell().Element(BodyCell).Text(text.User(transfer.Creditor));
                table.Cell().Element(BodyCell).AlignRight().Text(text.Amount(transfer.Amount)).Bold();
            }
        });
    }

    private static void GroupExpenseTable(IContainer container, GroupDebtReport report, ReportText text)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(60);
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
                columns.RelativeColumn(3);
                columns.ConstantColumn(72);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text(text["date"]);
                header.Cell().Element(HeaderCell).Text(text["description"]);
                header.Cell().Element(HeaderCell).Text(text["paidBy"]);
                header.Cell().Element(HeaderCell).Text(text["shares"]);
                header.Cell().Element(HeaderCell).AlignRight().Text(text["amount"]);
            });

            foreach (var expense in report.Expenses)
            {
                table.Cell().Element(BodyCell).Text(text.Date(expense.Date));
                table.Cell().Element(BodyCell).Column(cell =>
                {
                    var description = string.IsNullOrWhiteSpace(expense.Description) ? expense.Category : expense.Description;
                    cell.Item().Text(text.User(description));
                    cell.Item().Text(text.User(expense.Category)).FontSize(7.5f).FontColor(Palette.Muted);
                });
                table.Cell().Element(BodyCell).Text(text.User(expense.PaidBy));
                table.Cell().Element(BodyCell).Column(cell =>
                {
                    cell.Item().Text(text.SplitTypeName(expense.SplitType)).FontSize(7.5f).FontColor(Palette.Muted);
                    cell.Item().Text(string.Join(", ", expense.Shares.Select(s => $"{text.User(s.Name)} {text.Amount(s.Amount)}")))
                        .FontSize(8);
                });
                table.Cell().Element(BodyCell).AlignRight().Text(text.Amount(expense.Amount)).Bold();
            }
        });
    }

    // ======================================================================
    // Shared building blocks
    // ======================================================================

    private static void SetUpPage(PageDescriptor page, string title, ReportText text, DateTime generatedAtUtc)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(40);
        page.MarginVertical(32);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(style => style
            .FontFamily(QuestPdfSetup.FontFamily)
            .FontSize(9)
            .LineHeight(1.25f)
            .FontColor(Palette.Text));

        page.Header().PaddingBottom(16).BorderBottom(0.75f).BorderColor(Palette.Divider).PaddingBottom(8).Row(row =>
        {
            row.RelativeItem().Text("FairShare").FontSize(12).Bold().FontColor(Palette.Primary);
            row.RelativeItem().AlignRight().AlignBottom().Text(title).FontSize(8).FontColor(Palette.Muted);
        });

        page.Footer().PaddingTop(10).BorderTop(0.75f).BorderColor(Palette.Divider).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text($"{text["generated"]}: {text.Moment(generatedAtUtc)}").FontSize(7.5f).FontColor(Palette.Muted);
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.DefaultTextStyle(style => style.FontSize(7.5f).FontColor(Palette.Muted));
                t.Span(text["page"] + " ");
                t.CurrentPageNumber();
                t.Span($" {text["of"]} ");
                t.TotalPages();
            });
        });
    }

    private static void Title(ColumnDescriptor column, string title, string subtitle, string note)
    {
        column.Item().Text(title).FontSize(20).Bold();
        column.Item().PaddingTop(2).Text(subtitle).FontSize(10.5f);
        column.Item().PaddingTop(1).Text(note).FontSize(8.5f).FontColor(Palette.Muted);
    }

    private static void Section(ColumnDescriptor column, string title, string? hint = null)
    {
        // A heading is never left alone at the bottom of a page: with less than 90 pt of space
        // left, it moves to the next page together with its table.
        column.Item().PaddingTop(22).EnsureSpace(90).Column(heading =>
        {
            heading.Item().Text(title).FontSize(12.5f).Bold();
            if (hint is not null)
                heading.Item().PaddingTop(1).Text(hint).FontSize(8).FontColor(Palette.Muted);
            heading.Item().Height(8);
        });
    }

    /// <summary>A summary tile: label, large value and an optional note.</summary>
    private static void Kpi(IContainer container, string label, string value, Color? color = null, string? note = null)
    {
        container.Background(Palette.Surface).CornerRadius(6).Padding(10).Column(column =>
        {
            column.Spacing(3);
            column.Item().Text(label).FontSize(7.5f).FontColor(Palette.Muted);
            column.Item().Text(value).FontSize(13).Bold().FontColor(color ?? Palette.Text);
            if (note is not null)
                column.Item().Text(note).FontSize(7).FontColor(Palette.Muted);
        });
    }

    private static void ShareBar(IContainer container, decimal percentage, ReportText text)
    {
        var share = (float)Math.Clamp(percentage, 0m, 100m);

        container.Row(row =>
        {
            row.Spacing(6);
            row.RelativeItem().AlignMiddle().Row(bar =>
            {
                if (share > 0)
                    bar.RelativeItem(share).Height(5).Background(Palette.Primary);
                if (share < 100)
                    bar.RelativeItem(100 - share).Height(5).Background(Palette.Track);
            });
            row.ConstantItem(36).AlignRight().Text(text.Percent(percentage)).FontSize(8).FontColor(Palette.Muted);
        });
    }

    private static IContainer HeaderCell(IContainer container)
        => container
            .BorderBottom(0.75f).BorderColor(Palette.Border)
            .PaddingVertical(5).PaddingHorizontal(4)
            .DefaultTextStyle(style => style.FontSize(7.5f).Bold().FontColor(Palette.Muted));

    private static IContainer BodyCell(IContainer container)
        => container
            .BorderBottom(0.5f).BorderColor(Palette.Divider)
            .PaddingVertical(5).PaddingHorizontal(4);

    private static IContainer Note(IContainer container)
        => container.Background(Palette.Surface).CornerRadius(6).Padding(10);

    private static Color BalanceColor(decimal balance)
        => balance > GroupBalanceCalculator.Tolerance ? Palette.Primary
            : balance < -GroupBalanceCalculator.Tolerance ? Palette.Negative
            : Palette.Muted;

    /// <summary>"+25,00" or "−25,00" (a real minus sign); a settled balance has no sign.</summary>
    private static string SignedAmount(decimal value, ReportText text)
    {
        if (GroupBalanceCalculator.IsSettled(value))
            return text.Amount(0m);
        return (value > 0 ? "+" : "−") + text.Amount(Math.Abs(value));
    }

    private static DocumentMetadata Metadata(string title, ReportText text) => new()
    {
        Title = title,
        Author = "FairShare",
        Creator = "FairShare",
        Language = text.LanguageTag,
        CreationDate = DateTimeOffset.UtcNow,
        ModifiedDate = DateTimeOffset.UtcNow
    };

    /// <summary>The web app's colors (tokens.css, light theme).</summary>
    private static class Palette
    {
        public static readonly Color Text = Color.FromHex("#17242a");
        public static readonly Color Muted = Color.FromHex("#4a5a60");
        public static readonly Color Border = Color.FromHex("#dce3e1");
        public static readonly Color Divider = Color.FromHex("#e5ebe9");
        public static readonly Color Surface = Color.FromHex("#f3f5f4");
        public static readonly Color Track = Color.FromHex("#e9edec");
        public static readonly Color Primary = Color.FromHex("#0b6375");
        public static readonly Color Negative = Color.FromHex("#b45309");
    }
}
