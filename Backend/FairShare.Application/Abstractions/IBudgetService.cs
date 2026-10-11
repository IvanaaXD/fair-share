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

    /// <summary>Current user's budgets for the given month, with calculated current spending.</summary>
    Task<IReadOnlyList<BudgetResponse>> GetMyBudgetsAsync(
    Guid currentUserId,
        string month,
        CancellationToken cancellationToken = default);
}
