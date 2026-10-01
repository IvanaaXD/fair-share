namespace FairShare.Domain.Services;

/// <summary>Приједлог једне трансакције поравнања: дужник плаћа повјериоцу Amount.</summary>
public record SettlementSuggestion(Guid DebtorUserId, Guid CreditorUserId, decimal Amount);

/// <summary>
/// Чиста доменска логика за поједностављивање дугова (функционалност 5.7). Улаз су
/// нето салда чланова групе (позитивно = потражује, негативно = дугује), а излаз је
/// минималан скуп трансакција које своде сва салда на нулу.
/// </summary>
public static class DebtSimplifier
{
    private const decimal Tolerance = 0.01m;

    /// <summary>Највећи број учесника (са ненултим салдом) за који се покреће егзактни алгоритам.</summary>
    private const int MaxExactParticipants = 12;

    /// <summary>
    /// Хеуристички (greedy) приступ: у сваком кораку спаја тренутно највећег дужника
    /// са тренутно највећим повјериоцем. Гарантује највише n-1 трансакција за n
    /// учесника са ненултим салдом. Сложеност: O(n log n).
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
    /// Егзактно рјешење проблема оптималног поравнања дугова (Optimal Account
    /// Balancing) - NP-тежак проблем. Бектрекинг са одсијецањем грана (branch and
    /// bound): редом покушава да "спари" текућег учесника са сваким сљедећим
    /// супротног знака, преноси преостали износ, и памти најкраћи пронађени низ
    /// трансакција. Примјењиво само на мање групе (<see cref="MaxExactParticipants"/>),
    /// јер је сложеност у најгорем случају експоненцијална.
    /// </summary>
    public static List<SettlementSuggestion> SimplifyExact(IReadOnlyDictionary<Guid, decimal> netBalances)
    {
        var participants = netBalances.Where(kv => Math.Abs(kv.Value) > Tolerance).ToList();
        if (participants.Count == 0)
            return new List<SettlementSuggestion>();

        if (participants.Count > MaxExactParticipants)
            throw new InvalidOperationException(
                $"Егзактни алгоритам је примјењив само на мање групе (до {MaxExactParticipants} " +
                $"учесника са ненултим салдом, тренутно: {participants.Count}).");

        var ids = participants.Select(p => p.Key).ToArray();

        // Износи се своде на цијели број (центи) ради прецизности током рекурзије
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

        // Одсијецање грана: ако смо већ на дужини најбољег пронађеног рјешења,
        // наставак ове гране не може дати боље рјешење.
        if (best.Count > 0 && current.Count >= best.Count)
            return;

        for (var i = start + 1; i < amounts.Length; i++)
        {
            if (amounts[i] == 0) continue;
            if ((amounts[start] > 0) == (amounts[i] > 0)) continue; // исти знак - не могу поравнати једно друго

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
