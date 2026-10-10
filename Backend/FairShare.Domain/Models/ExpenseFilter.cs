namespace FairShare.Domain.Models;

public enum ExpenseSortField
{
    Date,
    Amount
}

/// <summary>
/// Search criteria for a user's expenses, as the repository needs them: dates already in UTC
/// and the end of the period exclusive. Built by ExpenseService from the query string.
/// </summary>
public sealed record ExpenseFilter
{
    public Guid UserId { get; init; }

    /// <summary>Inclusive start of the period.</summary>
    public DateTime? From { get; init; }

    /// <summary>Exclusive end of the period (start of the day after the last included day).</summary>
    public DateTime? ToExclusive { get; init; }

    public Guid? CategoryId { get; init; }

    /// <summary>Text searched in the description and the category name, ignoring case.</summary>
    public string? Search { get; init; }

    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public bool? IsRecurring { get; init; }

    public ExpenseSortField SortBy { get; init; } = ExpenseSortField.Date;
    public bool SortDescending { get; init; } = true;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>One page of results plus the number of all results that match the filter.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);
