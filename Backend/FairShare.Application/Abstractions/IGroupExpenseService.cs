using FairShare.Application.DTOs.GroupExpenses;

namespace FairShare.Application.Interfaces;

public interface IGroupExpenseService
{
    /// <summary>Adds a group expense and generates its splits. Members only.</summary>
    Task<GroupExpenseResponse> CreateAsync(
        Guid groupId,
        CreateGroupExpenseRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>One group expense with its splits. Members only.</summary>
    Task<GroupExpenseResponse> GetByIdAsync(
        Guid groupId,
        Guid groupExpenseId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Paged list of the group's expenses, newest first. Members only.</summary>
    Task<IReadOnlyList<GroupExpenseResponse>> GetByGroupAsync(
        Guid groupId,
        int page,
        int pageSize,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the expense data and recalculates its splits. Allowed for the member who paid
    /// the expense and for the group owner.
    /// </summary>
    Task<GroupExpenseResponse> UpdateAsync(
        Guid groupId,
        Guid groupExpenseId,
        UpdateGroupExpenseRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes the expense with its splits and comments. Same permissions as update.</summary>
    Task DeleteAsync(
        Guid groupId,
        Guid groupExpenseId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}
