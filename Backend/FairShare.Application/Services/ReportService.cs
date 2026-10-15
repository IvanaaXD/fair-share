using System.Globalization;
using System.Text;
using FairShare.Application.Abstractions;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.Common.Reports;
using FairShare.Application.Common.Text;
using FairShare.Application.DTOs.Analytics;
using FairShare.Application.DTOs.Reports;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Models;
using FairShare.Domain.Services;

namespace FairShare.Application.Services;

/// <summary>
/// Collects the data of a report and hands it to the PDF or CSV writer. The numbers come from
/// the same places as in the app (analytics, balance calculator, debt simplifier), so a report
/// never disagrees with what the user sees on the screen.
/// </summary>
public class ReportService : IReportService
{
    private const string PdfContentType = "application/pdf";
    private const string CsvContentType = "text/csv; charset=utf-8";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IAnalyticsService _analytics;
    private readonly IExchangeRateProvider _exchangeRates;
    private readonly IPdfReportRenderer _pdf;

    public ReportService(
        IUnitOfWork unitOfWork,
        IAnalyticsService analytics,
        IExchangeRateProvider exchangeRates,
        IPdfReportRenderer pdf)
    {
        _unitOfWork = unitOfWork;
        _analytics = analytics;
        _exchangeRates = exchangeRates;
        _pdf = pdf;
    }

    public async Task<ReportFile> GetSpendingReportAsync(
        Guid currentUserId,
        AnalyticsPeriod period,
        DateTime? from,
        DateTime? to,
        string? currency,
        ReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException("Корисник није пронађен.", ErrorCodes.UserNotFound);

        // Also validates the period (custom dates, at most one year), exactly like the analytics page.
        var analytics = await _analytics.GetSpendingAnalyticsAsync(
            currentUserId, period, from, to, currency, cancellationToken);
        var rates = await _exchangeRates.GetRatesAsync(cancellationToken);

        var page = await _unitOfWork.Expenses.GetPagedByUserAsync(new ExpenseFilter
        {
            UserId = currentUserId,
            From = analytics.From,
            ToExclusive = analytics.To.AddDays(1),
            SortBy = ExpenseSortField.Date,
            SortDescending = false,
            Page = 1,
            PageSize = ReportLimits.MaxRows
        }, cancellationToken);

        var expenses = page.Items
            .Select(e => new SpendingReportExpense(
                e.Date,
                e.Category?.Name ?? string.Empty,
                e.Description,
                e.Amount,
                e.Currency,
                rates.TryConvert(e.Amount, e.Currency, analytics.Currency, out var converted) ? converted : null,
                e.IsRecurring))
            .ToList();

        var report = new SpendingReport
        {
            UserName = FullName(user),
            GeneratedAtUtc = DateTime.UtcNow,
            Analytics = analytics,
            Expenses = expenses,
            TotalExpenseCount = page.TotalCount
        };

        var text = ReportText.For(request.Language, request.TimeZone);
        var name = $"{FileBase(text, "fairshare-potrosnja", "fairshare-spending")}-{Iso(analytics.From)}-{Iso(analytics.To)}";

        return request.Format == ReportFormat.Csv
            ? new ReportFile(CsvReportWriter.WriteSpending(report, text), CsvContentType, name + ".csv")
            : new ReportFile(_pdf.RenderSpending(report, text), PdfContentType, name + ".pdf");
    }

    public async Task<ReportFile> GetGroupDebtReportAsync(
        Guid groupId,
        Guid currentUserId,
        ReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var group = await _unitOfWork.Groups.GetWithMembersAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Група није пронађена.", ErrorCodes.GroupNotFound);

        if (group.Members.All(m => m.UserId != currentUserId))
            throw new ForbiddenException("Само члан групе може преузети извјештај групе.", ErrorCodes.NotGroupMember);

        var usersById = group.Members.ToDictionary(m => m.UserId, m => m.User);

        // The same calculation as the group page: all expenses and completed settlements.
        var allExpenses = await _unitOfWork.GroupExpenses.GetAllByGroupAsync(groupId, cancellationToken);
        var settled = await _unitOfWork.SettlementTransactions.GetByGroupAsync(
            groupId, SettlementStatus.Settled, cancellationToken);
        var balances = GroupBalanceCalculator.Calculate(allExpenses, settled);

        // Like SettlementService: a former member with an open balance means the balances of the
        // current members do not add up to zero, and the algorithm cannot be used.
        var suggestionUnavailable = balances.Any(b =>
            !usersById.ContainsKey(b.Key) && !GroupBalanceCalculator.IsSettled(b.Value));

        var transfers = new List<GroupReportTransfer>();
        if (!suggestionUnavailable)
        {
            var memberBalances = balances
                .Where(b => usersById.ContainsKey(b.Key))
                .ToDictionary(b => b.Key, b => b.Value);

            // Calculated only for the report - nothing is saved and nobody is notified.
            transfers = DebtSimplifier.SimplifyGreedy(memberBalances)
                .Select(t => new GroupReportTransfer(
                    FullName(usersById[t.DebtorUserId]),
                    FullName(usersById[t.CreditorUserId]),
                    t.Amount))
                .ToList();
        }

        var text = ReportText.For(request.Language, request.TimeZone);

        // Expenses with category, payer and shares; newest first, like in the app.
        IReadOnlyList<GroupExpense> listed = allExpenses.Count == 0
            ? []
            : await _unitOfWork.GroupExpenses.GetByGroupAsync(groupId, 1, ReportLimits.MaxRows, cancellationToken);

        var report = new GroupDebtReport
        {
            GroupName = group.Name,
            Currency = group.Currency,
            RequestedBy = FullName(usersById[currentUserId]),
            GeneratedAtUtc = DateTime.UtcNow,
            Balances = group.Members
                .Select(m => new GroupReportBalance(
                    FullName(m.User),
                    Math.Round(balances.GetValueOrDefault(m.UserId, 0m), 2),
                    m.UserId == currentUserId))
                .OrderBy(b => b.Balance)
                .ThenBy(b => b.Name, StringComparer.CurrentCulture)
                .ToList(),
            SuggestedTransfers = transfers,
            SuggestionUnavailable = suggestionUnavailable,
            CompletedSettlements = settled
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new GroupReportTransfer(
                    FullName(s.DebtorUser), FullName(s.CreditorUser), s.Amount, s.CreatedAt))
                .ToList(),
            Expenses = listed
                .Select(e => new GroupReportExpense(
                    e.Date,
                    DescriptionWithOriginal(e, text),
                    e.Category?.Name ?? string.Empty,
                    FullName(e.PaidByUser),
                    e.Amount,
                    e.SplitType,
                    e.Splits
                        .Select(s => new GroupReportShare(FullName(s.User), s.Amount))
                        .ToList()))
                .ToList(),
            TotalExpenseCount = allExpenses.Count,
            TotalSpent = allExpenses.Sum(e => e.Amount)
        };

        var name = $"{FileBase(text, "fairshare-grupa", "fairshare-group")}-{Slug(group.Name)}-{Iso(DateTime.UtcNow)}";

        return request.Format == ReportFormat.Csv
            ? new ReportFile(CsvReportWriter.WriteGroupDebts(report, text), CsvContentType, name + ".csv")
            : new ReportFile(_pdf.RenderGroupDebts(report, text), PdfContentType, name + ".pdf");
    }

    // ---------- helpers ----------

    /// <summary>
    /// An expense entered in another currency shows what was actually paid next to its
    /// description, e.g. "Hotel (100.00 EUR)"; the amount column stays in the group currency.
    /// </summary>
    private static string? DescriptionWithOriginal(GroupExpense expense, ReportText text)
    {
        if (expense.OriginalAmount is not { } original || expense.OriginalCurrency is null)
            return expense.Description;

        var paid = $"({text.Money(original, expense.OriginalCurrency)})";
        return string.IsNullOrWhiteSpace(expense.Description) ? paid : $"{expense.Description} {paid}";
    }

    private static string FullName(User? user)
        => user is null ? string.Empty : $"{user.FirstName} {user.LastName}".Trim();

    private static string FileBase(ReportText text, string serbian, string english)
        => text.IsEnglish ? english : serbian;

    private static string Iso(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    /// File-name-safe version of a group name: Latin letters without diacritics, digits and
    /// dashes ("Стан Бања Лука" -> "stan-banja-luka"). Plain ASCII works in every browser.
    /// </summary>
    private static string Slug(string value)
    {
        var latin = CyrillicTransliterator.ToLatin(value).Replace("đ", "dj").Replace("Đ", "Dj");
        var builder = new StringBuilder();
        foreach (var c in latin.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsAsciiLetterOrDigit(c))
                builder.Append(char.ToLowerInvariant(c));
            else if (builder.Length > 0 && builder[^1] != '-')
                builder.Append('-');

            if (builder.Length >= 40)
                break;
        }

        var slug = builder.ToString().Trim('-');
        return slug.Length > 0 ? slug : "group";
    }
}
