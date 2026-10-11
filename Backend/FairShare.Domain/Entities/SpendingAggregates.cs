namespace FairShare.Domain.Models;

/// <summary>Total spending for a single day (result of database aggregation).</summary>
public record DailySpendingTotal(DateTime Day, decimal Total, int Count);

/// <summary>Total spending per category for a given period.</summary>
public record CategorySpendingTotal(Guid CategoryId, string CategoryName, decimal Total, int Count);

/// <summary>Expense with location, for display on a map (only fields needed for the map).</summary>
public record ExpenseLocation(
    Guid Id,
    decimal Amount,
    string Currency,
    DateTime Date,
    string? Description,
    string CategoryName,
    double Latitude,
    double Longitude);