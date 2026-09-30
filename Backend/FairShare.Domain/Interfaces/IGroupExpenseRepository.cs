using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IGroupExpenseRepository : IRepository<GroupExpense>
{
    /// <summary>Учитава групни трошак заједно са подјелама (за прерачун салда).</summary>
    Task<GroupExpense?> GetWithSplitsAsync(Guid groupExpenseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupExpense>> GetByGroupAsync(
        Guid groupId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
