using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Models;

/// <summary>Број записа за један дан (резултат агрегације у бази).</summary>
public record DailyCount(DateTime Day, int Count);

/// <summary>Укупно стање система (свих времена) за администраторску контролну таблу.</summary>
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

/// <summary>Серије које се приказују кроз вријеме на администраторској контролној табли.</summary>
public enum StatisticSeries
{
    NewUsers,
    NewGroups,
    PersonalExpenses,
    GroupExpenses,
    Settlements,
    Payments
}
