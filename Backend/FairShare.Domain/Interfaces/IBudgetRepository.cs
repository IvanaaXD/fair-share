using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IBudgetRepository : IRepository<Budget>
{
    Task<Budget?> GetByUserCategoryAndMonthAsync(
        Guid userId,
        Guid categoryId,
        string month,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Budget>> GetByUserAndMonthAsync(
        Guid userId,
        string month,
        CancellationToken cancellationToken = default);
}
