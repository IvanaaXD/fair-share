namespace FairShare.Application.DTOs.Analytics;

/// <summary>Фиксни избор периода или произвољан период (Custom, највише годину дана).</summary>
public enum AnalyticsPeriod
{
    LastWeek,
    LastMonth,
    Last3Months,
    Last6Months,
    LastYear,
    Custom
}

/// <summary>Ниво агрегације тачака на графику, бира се аутоматски према дужини периода.</summary>
public enum TimeGranularity
{
    Day,
    Week,
    Month
}
