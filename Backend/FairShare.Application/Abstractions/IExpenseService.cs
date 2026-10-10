using FairShare.Application.DTOs.Common;
using FairShare.Application.DTOs.Expenses;

namespace FairShare.Application.Interfaces;

public interface IExpenseService
{
    Task<ExpenseResponse> CreateAsync(
        CreateExpenseRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<ExpenseResponse> UpdateAsync(
        Guid expenseId,
        UpdateExpenseRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid expenseId, Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>NEW: one expense of the current user.</summary>
    Task<ExpenseResponse> GetByIdAsync(Guid expenseId, Guid currentUserId, CancellationToken cancellationToken = default);

    /// <summary>CHANGED: search, filtering, sorting and paging of the current user's expenses (5.3).</summary>
    Task<PagedResponse<ExpenseResponse>> GetMyExpensesAsync(
        Guid currentUserId,
        ExpenseQueryParameters query,
        CancellationToken cancellationToken = default);
}
