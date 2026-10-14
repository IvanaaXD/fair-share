namespace FairShare.Domain.Models;

/// <summary>Spending of one day in one currency (result of grouping in the database).</summary>
public record DailySpendingTotal(DateTime Day, string Currency, decimal Total, int Count);

/// <summary>Spending in one category and one currency for a period.</summary>
public record CategorySpendingTotal(Guid CategoryId, string CategoryName, string Currency, decimal Total, int Count);

/// <summary>Spending in one currency (e.g. a category in a month, for budgets).</summary>
public record CurrencyTotal(string Currency, decimal Total);

/// <summary>An expense with a location, for the map (only the fields the map needs).</summary>
public record ExpenseLocation(
    Guid Id,
    decimal Amount,
    string Currency,
    DateTime Date,
    string? Description,
    string CategoryName,
    double Latitude,
    double Longitude);
