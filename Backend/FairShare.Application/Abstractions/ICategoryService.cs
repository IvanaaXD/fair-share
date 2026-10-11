using FairShare.Application.DTOs.Categories;

namespace FairShare.Application.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates a new user category (always IsSystemDefined = false).</summary>
    Task<CategoryResponse> CreateAsync(
        CreateCategoryRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>System categories can only be modified by an administrator.</summary>
    Task<CategoryResponse> UpdateAsync(
        Guid categoryId,
        UpdateCategoryRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>System categories can only be deleted by an administrator; a category in use cannot be deleted.</summary>
    Task DeleteAsync(Guid categoryId, Guid currentUserId, CancellationToken cancellationToken = default);
}
