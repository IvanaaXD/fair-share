using FairShare.Application.DTOs.Budgets;

namespace FairShare.Application.Interfaces;

public interface IBudgetService
{
    Task<BudgetResponse> CreateAsync(
        CreateBudgetRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<BudgetResponse> UpdateAsync(
        Guid budgetId,
        UpdateBudgetRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid budgetId, Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>Буџети тренутног корисника за дати мјесец, са израчунатом тренутном потрошњом.</summary>
    Task<IReadOnlyList<BudgetResponse>> GetMyBudgetsAsync(
        Guid currentUserId,
        string month,
        CancellationToken cancellationToken = default);
}
