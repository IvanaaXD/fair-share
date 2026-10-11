using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Models;

/// <summary>Number of records for a single day (result of database aggregation).</summary>
public record DailyCount(DateTime Day, int Count);

/// <summary>Total system state (all time) for the administrator dashboard.</summary>
public record SystemTotals(
    int Users,
    int BlockedUsers,
    int Admins,
    int Groups,
    int PersonalExpenses,
    int GroupExpenses,
    int ProposedSettlements,
    int SettledSettlements,
    IReadOnlyDictionary<PaymentStatus, int> PaymentsByStatus);

/// <summary>Series displayed over time on the administrator dashboard.</summary>
public enum StatisticSeries
{
    NewUsers,
    NewGroups,
    PersonalExpenses,
    GroupExpenses,
    Settlements,
    Payments
}