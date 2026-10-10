using FairShare.Domain.Models;

namespace FairShare.Application.DTOs.Expenses;

/// <summary>
/// Query string of GET /api/expenses, e.g.
/// ?search=stan&amp;from=2026-10-01&amp;to=2026-10-31&amp;sortBy=Amount&amp;page=2
/// Every parameter is optional.
/// </summary>
public class ExpenseQueryParameters
{
    public const int MaxPageSize = 100;
    public const int MaxSearchLength = 100;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>First day of the period (inclusive). Only the date part is used.</summary>
    public DateTime? From { get; set; }

    /// <summary>Last day of the period (inclusive - the whole day counts). Only the date part is used.</summary>
    public DateTime? To { get; set; }

    public Guid? CategoryId { get; set; }

    /// <summary>Text searched in the description and the category name.</summary>
    public string? Search { get; set; }

    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }

    /// <summary>true = only recurring expenses (templates), false = only the others.</summary>
    public bool? IsRecurring { get; set; }

    public ExpenseSortField SortBy { get; set; } = ExpenseSortField.Date;
    public bool SortDescending { get; set; } = true;
}
