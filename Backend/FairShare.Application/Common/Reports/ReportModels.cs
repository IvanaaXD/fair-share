using FairShare.Application.DTOs.Analytics;
using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.Common.Reports;

// Data of a report, independent of its format. ReportService fills them in, the PDF and CSV
// writers only lay them out - so both formats always contain the same numbers.

/// <summary>Personal spending in a period: the analytics summary plus every expense.</summary>
public sealed class SpendingReport
{
    public required string UserName { get; init; }
    public required DateTime GeneratedAtUtc { get; init; }

    /// <summary>The same numbers the analytics page shows (period, totals, categories, timeline).</summary>
    public required SpendingAnalyticsResponse Analytics { get; init; }

    /// <summary>Expenses of the period, oldest first. At most <see cref="ReportLimits.MaxRows"/>.</summary>
    public required IReadOnlyList<SpendingReportExpense> Expenses { get; init; }

    /// <summary>Number of all expenses in the period - larger than Expenses.Count if the list was cut.</summary>
    public required int TotalExpenseCount { get; init; }
}

/// <param name="ConvertedAmount">Amount in the report currency; null if no rate was known.</param>
public sealed record SpendingReportExpense(
    DateTime Date,
    string Category,
    string? Description,
    decimal Amount,
    string Currency,
    decimal? ConvertedAmount,
    bool IsRecurring);

/// <summary>Who owes whom in a group, how to settle it, and what the group spent.</summary>
public sealed class GroupDebtReport
{
    public required string GroupName { get; init; }
    public required string Currency { get; init; }
    public required string RequestedBy { get; init; }
    public required DateTime GeneratedAtUtc { get; init; }

    public required IReadOnlyList<GroupReportBalance> Balances { get; init; }

    /// <summary>Fewest payments that settle every balance, calculated when the report is made.</summary>
    public required IReadOnlyList<GroupReportTransfer> SuggestedTransfers { get; init; }

    /// <summary>True when a former member still has a balance, so no suggestion can be calculated.</summary>
    public required bool SuggestionUnavailable { get; init; }

    public required IReadOnlyList<GroupReportTransfer> CompletedSettlements { get; init; }

    /// <summary>Expenses of the group, newest first. At most <see cref="ReportLimits.MaxRows"/>.</summary>
    public required IReadOnlyList<GroupReportExpense> Expenses { get; init; }
    public required int TotalExpenseCount { get; init; }
    public required decimal TotalSpent { get; init; }
}

/// <param name="Balance">Positive: the group owes the member. Negative: the member owes the group.</param>
public sealed record GroupReportBalance(string Name, decimal Balance, bool IsCurrentUser);

/// <param name="Date">Only for completed settlements.</param>
public sealed record GroupReportTransfer(string Debtor, string Creditor, decimal Amount, DateTime? Date = null);

public sealed record GroupReportExpense(
    DateTime Date,
    string? Description,
    string Category,
    string PaidBy,
    decimal Amount,
    SplitType SplitType,
    IReadOnlyList<GroupReportShare> Shares);

public sealed record GroupReportShare(string Name, decimal Amount);

public static class ReportLimits
{
    /// <summary>
    /// Upper bound of listed expenses, so one request cannot produce a document of thousands of
    /// pages. The totals are always calculated from all expenses.
    /// </summary>
    public const int MaxRows = 2000;
}
