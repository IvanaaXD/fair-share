using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Services;

/// <summary>Улазни податак за једног учесника подјеле - amount/percentage су попуњени само за одговарајући SplitType.</summary>
public record SplitParticipantInput(Guid UserId, decimal? Amount = null, double? Percentage = null);

/// <summary>
/// Чиста доменска логика за подјелу групног трошка (функционалност 5.6). Не зависи
/// ни од базе ни од апликационог слоја - само рачуна и враћа ExpenseSplit ентитете.
/// </summary>
public static class ExpenseSplitCalculator
{
    private const decimal ExactAmountTolerance = 0.01m;
    private const double PercentageTolerance = 0.01;

    public static List<ExpenseSplit> Calculate(
        decimal totalAmount,
        SplitType splitType,
        IReadOnlyList<SplitParticipantInput> participants)
    {
        if (participants.Count < 2)
            throw new InvalidOperationException("Групни трошак мора имати најмање два учесника.");

        if (totalAmount <= 0)
            throw new InvalidOperationException("Износ трошка мора бити већи од нуле.");

        return splitType switch
        {
            SplitType.Equal => CalculateEqual(totalAmount, participants),
            SplitType.Percentage => CalculatePercentage(totalAmount, participants),
            SplitType.ExactAmount => CalculateExact(totalAmount, participants),
            _ => throw new ArgumentOutOfRangeException(nameof(splitType), "Непозната врста подјеле трошка.")
        };
    }

    private static List<ExpenseSplit> CalculateEqual(
        decimal totalAmount,
        IReadOnlyList<SplitParticipantInput> participants)
    {
        var count = participants.Count;
        var baseShare = Math.Round(totalAmount / count, 2, MidpointRounding.ToEven);

        var splits = participants
            .Select(p => new ExpenseSplit { UserId = p.UserId, Amount = baseShare })
            .ToList();

        // Остатак настао заокруживањем (нпр. 10 / 3 = 3.33...) додјељује се посљедњем учеснику
        var remainder = totalAmount - baseShare * count;
        if (remainder != 0)
            splits[^1].Amount += remainder;

        return splits;
    }

    private static List<ExpenseSplit> CalculatePercentage(
        decimal totalAmount,
        IReadOnlyList<SplitParticipantInput> participants)
    {
        if (participants.Any(p => p.Percentage is null or <= 0))
            throw new InvalidOperationException("Сваки учесник мора имати проценат већи од нуле.");

        var totalPercentage = participants.Sum(p => p.Percentage!.Value);
        if (Math.Abs(totalPercentage - 100) > PercentageTolerance)
            throw new InvalidOperationException($"Збир процената мора бити 100% (тренутно: {totalPercentage}%).");

        var splits = participants
            .Select(p => new ExpenseSplit
            {
                UserId = p.UserId,
                Percentage = p.Percentage,
                Amount = Math.Round(totalAmount * (decimal)p.Percentage!.Value / 100m, 2, MidpointRounding.ToEven)
            })
            .ToList();

        var remainder = totalAmount - splits.Sum(s => s.Amount);
        if (remainder != 0)
            splits[^1].Amount += remainder;

        return splits;
    }

    private static List<ExpenseSplit> CalculateExact(
        decimal totalAmount,
        IReadOnlyList<SplitParticipantInput> participants)
    {
        if (participants.Any(p => p.Amount is null or <= 0))
            throw new InvalidOperationException("Сваки учесник мора имати тачан износ већи од нуле.");

        var sum = participants.Sum(p => p.Amount!.Value);
        if (Math.Abs(sum - totalAmount) > ExactAmountTolerance)
            throw new InvalidOperationException(
                $"Збир тачних износа ({sum}) мора бити једнак укупном износу трошка ({totalAmount}).");

        return participants
            .Select(p => new ExpenseSplit { UserId = p.UserId, Amount = p.Amount!.Value })
            .ToList();
    }
}
