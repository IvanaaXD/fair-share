namespace FairShare.Application.DTOs.Analytics;

public class SpendingAnalyticsResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public TimeGranularity Granularity { get; set; }

    public decimal TotalSpent { get; set; }
    public int ExpenseCount { get; set; }

    /// <summary>Претходни период исте дужине, непосредно прије изабраног.</summary>
    public DateTime PreviousFrom { get; set; }
    public DateTime PreviousTo { get; set; }
    public decimal PreviousPeriodTotal { get; set; }

    /// <summary>Промјена у односу на претходни период у процентима; null ако у претходном периоду није било потрошње.</summary>
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

    /// <summary>Удио категорије у укупној потрошњи периода (0-100).</summary>
    public decimal Percentage { get; set; }

    public decimal PreviousPeriodTotal { get; set; }
}
