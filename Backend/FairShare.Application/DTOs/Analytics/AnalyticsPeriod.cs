namespace FairShare.Application.DTOs.Analytics;

/// <summary>Fixed period selection or custom period (Custom, maximum one year).</summary>
public enum AnalyticsPeriod
{
    LastWeek,
    LastMonth,
    Last3Months,
    Last6Months,
    LastYear,
    Custom
}

/// <summary>Aggregation level of points on the chart, chosen automatically based on the period length.</summary>
public enum TimeGranularity
{
    Day,
    Week,
    Month
}
