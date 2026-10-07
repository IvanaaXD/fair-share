using FairShare.Domain.Entities;

namespace FairShare.Domain.Services;

/// <summary>
/// Pure domain logic for group balances. Used by settlements (who owes whom) and by
/// group membership rules (a member can leave only when their balance is settled).
/// </summary>
public static class GroupBalanceCalculator
{
    /// <summary>Balances within one cent of zero are treated as settled (same tolerance as DebtSimplifier).</summary>
    public const decimal Tolerance = 0.01m;

    /// <summary>
    /// Net balance per user = everything the user paid for group expenses, minus everything
    /// the user owes through splits, adjusted by settlements that were already completed.
    /// Positive = the group owes the user, negative = the user owes the group.
    /// The balances of all users always add up to zero.
    /// </summary>
    /// <param name="expenses">All expenses of the group, with their Splits loaded.</param>
    /// <param name="settledTransactions">Settlement transactions of the group with status Settled.</param>
    public static Dictionary<Guid, decimal> Calculate(
        IEnumerable<GroupExpense> expenses,
        IEnumerable<SettlementTransaction> settledTransactions)
    {
        var balances = new Dictionary<Guid, decimal>();

        foreach (var expense in expenses)
        {
            Add(balances, expense.PaidByUserId, expense.Amount);

            foreach (var split in expense.Splits)
                Add(balances, split.UserId, -split.Amount);
        }

        // A completed settlement moves money from the debtor to the creditor.
        foreach (var settlement in settledTransactions)
        {
            Add(balances, settlement.DebtorUserId, settlement.Amount);
            Add(balances, settlement.CreditorUserId, -settlement.Amount);
        }

        return balances;
    }

    public static bool IsSettled(decimal balance) => Math.Abs(balance) <= Tolerance;

    private static void Add(Dictionary<Guid, decimal> balances, Guid userId, decimal amount)
        => balances[userId] = balances.GetValueOrDefault(userId, 0m) + amount;
}
