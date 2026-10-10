namespace FairShare.Application.Interfaces;

/// <summary>Automatic copies of recurring expenses; called by the background job, not by a controller.</summary>
public interface IRecurringExpenseService
{
    /// <summary>Ids of recurring expenses that have a copy due (or still need to be scheduled).</summary>
    Task<IReadOnlyList<Guid>> GetDueExpenseIdsAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Creates every copy of the expense that is due by <paramref name="nowUtc"/>; returns how many.</summary>
    Task<int> GenerateDueOccurrencesAsync(Guid expenseId, DateTime nowUtc, CancellationToken cancellationToken = default);
}
