using FairShare.Application.DTOs.Analytics;

namespace FairShare.Application.DTOs.Admin;

public class AdminStatisticsResponse
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public TimeGranularity Granularity { get; set; }

    /// <summary>Укупно стање система (свих времена, не само изабраног периода).</summary>
    public AdminTotalsResponse Totals { get; set; } = new();

    /// <summary>Број нових записа по кантици (дан/седмица/мјесец) у изабраном периоду.</summary>
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

    /// <summary>Кључ је назив статуса (Pending, Succeeded, Declined, Failed).</summary>
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
