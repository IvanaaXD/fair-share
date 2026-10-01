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

    /// <summary>Претрага и филтрирање личних трошкова тренутног корисника (функционалност 5.3).</summary>
    Task<IReadOnlyList<ExpenseResponse>> GetMyExpensesAsync(
        Guid currentUserId,
        DateTime? from = null,
        DateTime? to = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);
}
