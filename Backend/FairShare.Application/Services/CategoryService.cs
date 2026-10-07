using AutoMapper;
using FairShare.Application.Common.Exceptions;
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
    private readonly IMapper _mapper;

    public CategoryService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);

        // System categories first, then alphabetical.
        var ordered = categories
            .OrderByDescending(c => c.IsSystemDefined)
            .ThenBy(c => c.Name);

        return _mapper.Map<List<CategoryResponse>>(ordered);
    }

    public async Task<CategoryResponse> CreateAsync(
        CreateCategoryRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        if (await _unitOfWork.Categories.GetByNameAsync(name, cancellationToken) is not null)
            throw new ConflictException($"Категорија са називом '{name}' већ постоји.");

        var category = _mapper.Map<Category>(request);
        category.IsSystemDefined = false; // users can never create system categories

        await _unitOfWork.Categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CategoryResponse>(category);
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

        var name = request.Name.Trim();
        var existing = await _unitOfWork.Categories.GetByNameAsync(name, cancellationToken);
        if (existing is not null && existing.Id != categoryId)
            throw new ConflictException($"Категорија са називом '{name}' већ постоји.");

        _mapper.Map(request, category);

        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CategoryResponse>(category);
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
}
