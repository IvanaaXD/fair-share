using FairShare.Application.DTOs.GroupExpenses;

namespace FairShare.Application.Interfaces;

public interface IGroupExpenseService
{
    /// <summary>Додаје групни трошак и генерише одговарајуће ExpenseSplit записе (функционалност 5.6).</summary>
    Task<GroupExpenseResponse> CreateAsync(
        Guid groupId,
        CreateGroupExpenseRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupExpenseResponse>> GetByGroupAsync(
        Guid groupId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
