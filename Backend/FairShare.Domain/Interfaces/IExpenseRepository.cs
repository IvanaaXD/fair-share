using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IExpenseRepository : IRepository<Expense>
{
    /// <summary>Претрага и филтрирање личних трошкова (функционалност 5.3).</summary>
    Task<IReadOnlyList<Expense>> GetByUserAsync(
        Guid userId,
        DateTime? from = null,
        DateTime? to = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Суме потрошње по категоријама за аналитику (функционалност 5.4).</summary>
    Task<decimal> GetTotalByUserAndCategoryAsync(
        Guid userId,
        Guid categoryId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>Понављајући трошкови који доспијевају за аутоматско додавање.</summary>
    Task<IReadOnlyList<Expense>> GetRecurringDueAsync(DateTime asOf, CancellationToken cancellationToken = default);
}
