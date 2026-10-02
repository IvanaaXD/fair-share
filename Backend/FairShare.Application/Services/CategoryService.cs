using FairShare.Application.DTOs.Categories;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using TimeSheet.Application.Common.Exceptions;

namespace FairShare.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
        return categories
            .OrderByDescending(c => c.IsSystemDefined)
            .ThenBy(c => c.Name)
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<CategoryResponse> CreateAsync(
        CreateCategoryRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ConflictException("Назив категорије је обавезан.");

        var existing = await _unitOfWork.Categories.GetByNameAsync(request.Name.Trim(), cancellationToken);
        if (existing is not null)
            throw new ConflictException($"Категорија са називом '{request.Name}' већ постоји.");

        var category = new Category
        {
            Name = request.Name.Trim(),
            Icon = request.Icon,
            IsSystemDefined = false // корисници никад не могу креирати системску категорију
        };

        await _unitOfWork.Categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(category);
    }

    public async Task<CategoryResponse> UpdateAsync(
        Guid categoryId,
        UpdateCategoryRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new NotFoundException("Категорија није пронађена.");

        if (category.IsSystemDefined && !await IsAdminAsync(currentUserId, cancellationToken))
            throw new ForbiddenException("Само администратор може мијењати системске категорије.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ConflictException("Назив категорије је обавезан.");

        var existing = await _unitOfWork.Categories.GetByNameAsync(request.Name.Trim(), cancellationToken);
        if (existing is not null && existing.Id != categoryId)
            throw new ConflictException($"Категорија са називом '{request.Name}' већ постоји.");

        category.Name = request.Name.Trim();
        category.Icon = request.Icon;

        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(category);
    }

    public async Task DeleteAsync(Guid categoryId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new NotFoundException("Категорија није пронађена.");

        if (category.IsSystemDefined && !await IsAdminAsync(currentUserId, cancellationToken))
            throw new ForbiddenException("Само администратор може брисати системске категорије.");

        if (await _unitOfWork.Categories.IsInUseAsync(categoryId, cancellationToken))
            throw new ConflictException("Категорија се не може обрисати јер се користи у постојећим трошковима или буџетима.");

        _unitOfWork.Categories.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        return user?.Role == UserRole.Admin;
    }

    private static CategoryResponse MapToResponse(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Icon = category.Icon,
        IsSystemDefined = category.IsSystemDefined
    };
}
