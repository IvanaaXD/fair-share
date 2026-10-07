namespace FairShare.Domain.Models;

/// <summary>Укупна потрошња за један дан (резултат агрегације у бази).</summary>
public record DailySpendingTotal(DateTime Day, decimal Total, int Count);

/// <summary>Укупна потрошња по категорији за дати период.</summary>
public record CategorySpendingTotal(Guid CategoryId, string CategoryName, decimal Total, int Count);

/// <summary>Трошак са локацијом, за приказ на мапи (само поља потребна мапи).</summary>
public record ExpenseLocation(
    Guid Id,
    decimal Amount,
    string Currency,
    DateTime Date,
    string? Description,
    string CategoryName,
    double Latitude,
    double Longitude);
