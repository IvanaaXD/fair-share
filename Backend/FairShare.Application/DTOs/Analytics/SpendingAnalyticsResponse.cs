namespace FairShare.Application.DTOs.Analytics;

public class SpendingAnalyticsResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public TimeGranularity Granularity { get; set; }

    public decimal TotalSpent { get; set; }
    public int ExpenseCount { get; set; }

    /// <summary>Previous period of the same length, immediately before the selected one.</summary>
    public DateTime PreviousFrom { get; set; }
    public DateTime PreviousTo { get; set; }
    public decimal PreviousPeriodTotal { get; set; }

    /// <summary>Percentage change compared to the previous period; null if there was no spending in the previous period.</summary>
    public decimal? ChangePercentage { get; set; }
    public List<SpendingTimePoint> Timeline { get; set; } = new();
    public List<CategorySpendingResponse> ByCategory { get; set; } = new();
}

public class SpendingTimePoint
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal Total { get; set; }
}

public class CategorySpendingResponse
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Count { get; set; }

    /// <summary>Category's share in the total spending for the period (0-100).</summary>
    public decimal Percentage { get; set; }

    public decimal PreviousPeriodTotal { get; set; }
}
