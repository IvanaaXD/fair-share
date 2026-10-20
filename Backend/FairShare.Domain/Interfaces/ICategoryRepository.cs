using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IReadOnlyList<Category>> GetSystemDefinedAsync(CancellationToken cancellationToken = default);

    Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Approved categories - the list every user chooses from.</summary>
    Task<IReadOnlyList<Category>> GetApprovedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The category, only if it may be used for an expense or a budget (approved). A pending or
    /// rejected proposal is reported like a missing category.
    /// </summary>
    Task<Category?> GetUsableByIdAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>An approved or pending category with this name, ignoring letter case.</summary>
    Task<Category?> GetActiveByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<int> CountPendingByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>A user's own proposals, newest first.</summary>
    Task<IReadOnlyList<Category>> GetProposalsByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>For the administrator, with the proposer loaded; all statuses when null.</summary>
    Task<IReadOnlyList<Category>> GetForAdminAsync(CategoryStatus? status, CancellationToken cancellationToken = default);

    /// <summary>How many expenses, group expenses and budgets use each category (only used ones are listed).</summary>
    Task<IReadOnlyDictionary<Guid, int>> GetUsageCountsAsync(CancellationToken cancellationToken = default);

    // Checks if the category is used in expenses or budgets before deletion.
    Task<bool> IsInUseAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
