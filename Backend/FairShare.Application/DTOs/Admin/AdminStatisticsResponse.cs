using FairShare.Application.DTOs.Analytics;

namespace FairShare.Application.DTOs.Admin;

public class AdminStatisticsResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public TimeGranularity Granularity { get; set; }

    /// <summary>Overall system status (all-time, not just the selected period).</summary>
    public AdminTotalsResponse Totals { get; set; } = new();

    /// <summary>Number of new records per bucket (day/week/month) in the selected period.</summary>
    public List<AdminTimePoint> Timeline { get; set; } = new();
}

public class AdminTotalsResponse
{
    public int Users { get; set; }
    public int BlockedUsers { get; set; }
    public int Admins { get; set; }
    public int Groups { get; set; }
    public int PersonalExpenses { get; set; }
    public int GroupExpenses { get; set; }
    public int ProposedSettlements { get; set; }
    public int SettledSettlements { get; set; }

    /// <summary>The key is the status name (Pending, Succeeded, Declined, Failed).</summary>
    public Dictionary<string, int> PaymentsByStatus { get; set; } = new();
}

public class AdminTimePoint
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int NewUsers { get; set; }
    public int NewGroups { get; set; }
    public int PersonalExpenses { get; set; }
    public int GroupExpenses { get; set; }
    public int Settlements { get; set; }
    public int Payments { get; set; }
}
