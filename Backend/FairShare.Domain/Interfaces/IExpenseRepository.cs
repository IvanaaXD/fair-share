using FairShare.Domain.Entities;
using FairShare.Domain.Models;

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

    // ---------- НОВО: агрегације за аналитику (5.4) ----------
    // Сви интервали су [from, toExclusive) - почетак укључен, крај искључен.

    /// <summary>Укупна потрошња по данима; група по дану се рачуна у бази, не у меморији.</summary>
    Task<IReadOnlyList<DailySpendingTotal>> GetDailyTotalsAsync(
        Guid userId,
        DateTime from,
        DateTime toExclusive,
        CancellationToken cancellationToken = default);

    /// <summary>Укупна потрошња по категоријама у периоду.</summary>
    Task<IReadOnlyList<CategorySpendingTotal>> GetTotalsByCategoryAsync(
        Guid userId,
        DateTime from,
        DateTime toExclusive,
        CancellationToken cancellationToken = default);

    /// <summary>Најновији трошкови са локацијом у периоду, за приказ на мапи.</summary>
    Task<IReadOnlyList<ExpenseLocation>> GetLocationsAsync(
        Guid userId,
        DateTime from,
        DateTime toExclusive,
        int maxResults,
        CancellationToken cancellationToken = default);
}
