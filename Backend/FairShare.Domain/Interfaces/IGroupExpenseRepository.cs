using FairShare.Domain.Entities;

namespace FairShare.Domain.Interfaces;

public interface IGroupExpenseRepository : IRepository<GroupExpense>
{
    Task<GroupExpense?> GetWithSplitsAsync(Guid groupExpenseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupExpense>> GetByGroupAsync(
        Guid groupId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    // НОВО: све трошкове групе (без паginacije) са подјелама - користи се за
    // рачунање нето салда у SettlementService-у, гдје морамо прегледати баш све
    // трошкове, а не само једну страницу.
    Task<IReadOnlyList<GroupExpense>> GetAllByGroupAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);
}
