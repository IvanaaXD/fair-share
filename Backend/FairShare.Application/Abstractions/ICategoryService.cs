using FairShare.Application.DTOs.Categories;

namespace FairShare.Application.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Креира нову корисничку категорију (увијек IsSystemDefined = false).</summary>
    Task<CategoryResponse> CreateAsync(
        CreateCategoryRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Системске категорије може мијењати само администратор.</summary>
    Task<CategoryResponse> UpdateAsync(
        Guid categoryId,
        UpdateCategoryRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Системске категорије може брисати само администратор; категорија у употреби се не може обрисати.</summary>
    Task DeleteAsync(Guid categoryId, Guid currentUserId, CancellationToken cancellationToken = default);
}
