using FairShare.Application.DTOs.Categories;
using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.Interfaces;

/// <summary>
/// One list of categories for everyone, maintained by the administrator. A user can propose a
/// new category; it can be used only after an administrator approves it.
/// </summary>
public interface ICategoryService
{
    /// <summary>Approved categories - what every user chooses from.</summary>
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// An administrator adds an approved category at once; any other user sends a proposal
    /// that waits for approval (at most <see cref="Services.CategoryService.MaxPendingProposals"/> at a time).
    /// </summary>
    Task<CategoryProposalResponse> CreateAsync(
        CreateCategoryRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>The signed-in user's proposals and the decisions on them.</summary>
    Task<IReadOnlyList<CategoryProposalResponse>> GetMyProposalsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    // ---------- administrator only (the controller requires the Admin role) ----------

    Task<IReadOnlyList<AdminCategoryResponse>> GetForAdminAsync(
        CategoryStatus? status,
        CancellationToken cancellationToken = default);

    /// <summary>Approves a proposal; the administrator may correct its name and icon.</summary>
    Task<AdminCategoryResponse> ApproveAsync(
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryResponse> RejectAsync(
        Guid categoryId,
        RejectCategoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Renames an approved category or changes its icon.</summary>
    Task<AdminCategoryResponse> UpdateAsync(
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Only a category no expense, group expense or budget uses; a pending proposal is rejected instead.</summary>
    Task DeleteAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
