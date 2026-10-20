using AutoMapper;
using FairShare.Application.Abstractions;
using FairShare.Application.Common.Caching;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.Common.Notifications;
using FairShare.Application.DTOs.Categories;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;

namespace FairShare.Application.Services;

public class CategoryService : ICategoryService
{
    /// <summary>A user cannot flood the administrator with proposals.</summary>
    public const int MaxPendingProposals = 5;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;
    private readonly INotificationService _notifications;
    private readonly TimeProvider _time;

    public CategoryService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cache,
        INotificationService notifications,
        TimeProvider time)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cache = cache;
        _notifications = notifications;
        _time = time;
    }

    // The list is the same for every user, is loaded on almost every screen (expense forms,
    // filters, budgets) and changes rarely. When a category is saved, CacheInvalidationInterceptor
    // drops the cached list automatically - also when a proposal is approved.
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _cache.GetOrCreateAsync(
            CacheKeys.AllCategories,
            LoadAllAsync,
            CacheDurations.Categories,
            new[] { CacheTags.Categories },
            cancellationToken);

    private async Task<List<CategoryResponse>> LoadAllAsync(CancellationToken cancellationToken)
    {
        var categories = await _unitOfWork.Categories.GetApprovedAsync(cancellationToken);

        // Categories that came with the application first, then alphabetically.
        var ordered = categories
            .OrderByDescending(c => c.IsSystemDefined)
            .ThenBy(c => c.Name);

        return _mapper.Map<List<CategoryResponse>>(ordered);
    }

    public async Task<CategoryProposalResponse> CreateAsync(
        CreateCategoryRequest request,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        await EnsureNameIsFreeAsync(name, exceptId: null, cancellationToken);

        Category category;
        if (await IsAdminAsync(currentUserId, cancellationToken))
        {
            category = Category.CreateApproved(name, request.Icon);
        }
        else
        {
            var pending = await _unitOfWork.Categories.CountPendingByUserAsync(currentUserId, cancellationToken);
            if (pending >= MaxPendingProposals)
                throw new ConflictException(
                    $"Већ имаш {MaxPendingProposals} приједлога на чекању. Сачекај да их администратор прегледа.",
                    ErrorCodes.CategoryPendingLimit);

            category = Category.Propose(name, request.Icon, currentUserId, UtcNow);
        }

        await _unitOfWork.Categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<CategoryProposalResponse>(category);
    }

    public async Task<IReadOnlyList<CategoryProposalResponse>> GetMyProposalsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var proposals = await _unitOfWork.Categories.GetProposalsByUserAsync(currentUserId, cancellationToken);
        return _mapper.Map<List<CategoryProposalResponse>>(proposals);
    }

    // ---------- administrator ----------

    public async Task<IReadOnlyList<AdminCategoryResponse>> GetForAdminAsync(
        CategoryStatus? status,
        CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.GetForAdminAsync(status, cancellationToken);
        var usage = await _unitOfWork.Categories.GetUsageCountsAsync(cancellationToken);

        var result = _mapper.Map<List<AdminCategoryResponse>>(categories);
        foreach (var item in result)
            item.UsageCount = usage.GetValueOrDefault(item.Id);

        return result;
    }

    public async Task<AdminCategoryResponse> ApproveAsync(
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await GetPendingAsync(categoryId, cancellationToken);

        var name = request.Name.Trim();
        await EnsureNameIsFreeAsync(name, exceptId: category.Id, cancellationToken);

        category.Approve(name, request.Icon, UtcNow);
        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await NotifyProposerAsync(
            category,
            "Категорија је одобрена",
            $"Твој приједлог категорије {NotificationText.Quote(category.Name)} је одобрен. Сада је можеш бирати за трошкове и буџете.",
            cancellationToken);

        return await ToAdminResponseAsync(category, cancellationToken);
    }

    public async Task<AdminCategoryResponse> RejectAsync(
        Guid categoryId,
        RejectCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await GetPendingAsync(categoryId, cancellationToken);

        category.Reject(request.Reason.Trim(), UtcNow);
        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await NotifyProposerAsync(
            category,
            "Приједлог категорије није прихваћен",
            $"Твој приједлог категорије {NotificationText.Quote(category.Name)} није прихваћен. Разлог: {category.RejectionReason}",
            cancellationToken);

        return await ToAdminResponseAsync(category, cancellationToken);
    }

    public async Task<AdminCategoryResponse> UpdateAsync(
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await GetExistingAsync(categoryId, cancellationToken);
        if (!category.IsApproved)
            throw new ConflictException(
                "Мијењати се може само одобрена категорија; приједлог се одобрава или одбија.",
                ErrorCodes.CategoryNotPending);

        var name = request.Name.Trim();
        await EnsureNameIsFreeAsync(name, exceptId: category.Id, cancellationToken);

        // A rename shows everywhere at once: expenses keep the category, not a copy of its name.
        category.Name = name;
        category.Icon = request.Icon;
        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToAdminResponseAsync(category, cancellationToken);
    }

    public async Task DeleteAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await GetExistingAsync(categoryId, cancellationToken);

        // The user who proposed it should learn the decision, so a proposal is rejected, not deleted.
        if (category.IsPending)
            throw new ConflictException(
                "Приједлог на чекању се не брише – одобри га или одбиј, да предлагач добије одговор.",
                ErrorCodes.CategoryNotPending);

        if (await _unitOfWork.Categories.IsInUseAsync(categoryId, cancellationToken))
            throw new ConflictException(
                "Категорија се не може обрисати јер се користи у постојећим трошковима или буџетима.",
                ErrorCodes.CategoryInUse);

        _unitOfWork.Categories.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ---------- helpers ----------

    private DateTime UtcNow => _time.GetUtcNow().UtcDateTime;

    private async Task<Category> GetExistingAsync(Guid categoryId, CancellationToken cancellationToken)
        => await _unitOfWork.Categories.GetByIdAsync(categoryId, cancellationToken)
           ?? throw new NotFoundException("Категорија није пронађена.", ErrorCodes.CategoryNotFound);

    private async Task<Category> GetPendingAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await GetExistingAsync(categoryId, cancellationToken);
        if (!category.IsPending)
            throw new ConflictException("О овом приједлогу је већ одлучено.", ErrorCodes.CategoryNotPending);
        return category;
    }

    /// <summary>No two approved or pending categories may have the same name (letter case ignored).</summary>
    private async Task EnsureNameIsFreeAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.Categories.GetActiveByNameAsync(name, cancellationToken);
        if (existing is null || existing.Id == exceptId)
            return;

        throw new ConflictException(
            existing.IsPending
                ? $"Категорија {NotificationText.Quote(name)} је већ предложена и чека одобрење."
                : $"Категорија {NotificationText.Quote(name)} већ постоји.",
            ErrorCodes.CategoryExists);
    }

    private async Task NotifyProposerAsync(Category category, string subject, string message, CancellationToken cancellationToken)
    {
        if (category.ProposedByUserId is not { } userId)
            return;

        // In the app only (also live, over SignalR) - an e-mail would be too much for this.
        await _notifications.NotifyAsync(userId, NotificationType.CategoryReviewed, subject, message, cancellationToken, sendEmail: false);
    }

    private async Task<AdminCategoryResponse> ToAdminResponseAsync(Category category, CancellationToken cancellationToken)
    {
        var response = _mapper.Map<AdminCategoryResponse>(category);
        if (category.ProposedByUserId is { } userId && category.ProposedByUser is null)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
            response.ProposedByName = user is null ? null : $"{user.FirstName} {user.LastName}".Trim();
            response.ProposedByEmail = user?.Email;
        }
        return response;
    }

    private async Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        return user?.Role == UserRole.Admin;
    }
}
