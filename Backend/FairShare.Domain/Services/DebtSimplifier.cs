namespace FairShare.Domain.Services;

/// <summary>Proposal for a single settlement transaction: the debtor pays the creditor the Amount.</summary>
public record SettlementSuggestion(Guid DebtorUserId, Guid CreditorUserId, decimal Amount);

/// <summary>
/// Pure domain logic for debt simplification (feature 5.7). The input is the
/// net balances of group members (positive = claims, negative = owes), and the output is the
/// minimal set of transactions that reduces all balances to zero.
/// </summary>
public static class DebtSimplifier
{
    private const decimal Tolerance = 0.01m;

    /// <summary>The maximum number of participants (with a non-zero balance) for which the exact algorithm is run.</summary>
    private const int MaxExactParticipants = 12;

    /// <summary>
    /// Heuristic (greedy) approach: at each step, it pairs the current largest debtor
    /// with the current largest creditor. Guarantees at most n-1 transactions for n
    /// participants with a non-zero balance. Complexity: O(n log n).
    /// </summary>
    public static List<SettlementSuggestion> SimplifyGreedy(IReadOnlyDictionary<Guid, decimal> netBalances)
    {
        var debtors = new List<(Guid UserId, decimal Amount)>();
        var creditors = new List<(Guid UserId, decimal Amount)>();

        foreach (var (userId, balance) in netBalances)
        {
            if (balance < -Tolerance) debtors.Add((userId, -balance));
            else if (balance > Tolerance) creditors.Add((userId, balance));
        }

        debtors = debtors.OrderByDescending(d => d.Amount).ToList();
        creditors = creditors.OrderByDescending(c => c.Amount).ToList();

        var result = new List<SettlementSuggestion>();
        int i = 0, j = 0;

        while (i < debtors.Count && j < creditors.Count)
        {
            var (debtorId, debtAmount) = debtors[i];
            var (creditorId, creditAmount) = creditors[j];

            var settled = Math.Min(debtAmount, creditAmount);
            result.Add(new SettlementSuggestion(debtorId, creditorId, Math.Round(settled, 2)));

            debtAmount -= settled;
            creditAmount -= settled;
            debtors[i] = (debtorId, debtAmount);
            creditors[j] = (creditorId, creditAmount);

            if (debtAmount <= Tolerance) i++;
            if (creditAmount <= Tolerance) j++;
        }

        return result;
    }

    /// <summary>
    /// Exact solution to the Optimal Account Balancing problem - an NP-hard problem. 
    /// Backtracking with branch and bound: sequentially tries to "pair" the current participant 
    /// with every subsequent one of the opposite sign, carries over the remaining amount, 
    /// and keeps track of the shortest found sequence of transactions. Applicable only to smaller groups (<see cref="MaxExactParticipants"/>), 
    /// because the worst-case complexity is exponential.
    /// </summary>
    public static List<SettlementSuggestion> SimplifyExact(IReadOnlyDictionary<Guid, decimal> netBalances)
    {
        var participants = netBalances.Where(kv => Math.Abs(kv.Value) > Tolerance).ToList();
        if (participants.Count == 0)
            return new List<SettlementSuggestion>();

        if (participants.Count > MaxExactParticipants)
            throw new InvalidOperationException(
                $"The exact algorithm is applicable only to smaller groups (up to {MaxExactParticipants} " +
                $"participants with a non-zero balance, currently: {participants.Count}).");

        var ids = participants.Select(p => p.Key).ToArray();

        // Amounts are converted to integers (cents) for precision during recursion
        var amounts = participants.Select(p => (long)Math.Round(p.Value * 100)).ToArray();

        var best = new List<SettlementSuggestion>();
        var current = new List<SettlementSuggestion>();

        Backtrack(amounts, ids, 0, current, best);

        return best;
    }

    private static void Backtrack(
        long[] amounts,
        Guid[] ids,
        int start,
        List<SettlementSuggestion> current,
        List<SettlementSuggestion> best)
    {
        while (start < amounts.Length && amounts[start] == 0)
            start++;

        if (start == amounts.Length)
        {
            if (best.Count == 0 || current.Count < best.Count)
            {
                best.Clear();
                best.AddRange(current);
            }
            return;
        }

        // Branch pruning: if we are already at the length of the best found solution,
        // continuing this branch cannot yield a better solution.
        if (best.Count > 0 && current.Count >= best.Count)
            return;

        for (var i = start + 1; i < amounts.Length; i++)
        {
            if (amounts[i] == 0) continue;
            if ((amounts[start] > 0) == (amounts[i] > 0)) continue; // same sign - they cannot settle each other

            var amount = Math.Abs(amounts[start]);
            var debtorIndex = amounts[start] < 0 ? start : i;
            var creditorIndex = amounts[start] < 0 ? i : start;

            amounts[i] += amounts[start];
            current.Add(new SettlementSuggestion(ids[debtorIndex], ids[creditorIndex], amount / 100m));

            Backtrack(amounts, ids, start + 1, current, best);

            current.RemoveAt(current.Count - 1);
            amounts[i] -= amounts[start];
        }
    }
}