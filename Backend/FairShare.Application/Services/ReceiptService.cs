using FairShare.Application.Common.Exceptions;
using FairShare.Application.Common.Files;
using FairShare.Application.DTOs.Files;
using FairShare.Application.Interfaces;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;

namespace FairShare.Application.Services;

/// <summary>
/// Upload, download and removal of receipt photos.
///
/// Order of operations: on upload the file is saved first and the database second, so a failed
/// upload never leaves an expense pointing to a missing file. On removal the database goes first,
/// so a failed delete leaves at most an unused file, never a broken link.
/// </summary>
public class ReceiptService : IReceiptService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IImageStorageService _images;

    public ReceiptService(IUnitOfWork unitOfWork, IImageStorageService images)
    {
        _unitOfWork = unitOfWork;
        _images = images;
    }

    // ---------- personal expenses ----------

    public async Task<ImageUrlResponse> UploadExpenseReceiptAsync(
        Guid expenseId, Guid currentUserId, ImageUpload upload, CancellationToken cancellationToken = default)
    {
        var expense = await GetOwnExpenseAsync(expenseId, currentUserId, cancellationToken);

        await _images.SaveAsync(StorageKeys.ExpenseReceipt(expense.UserId, expense.Id), upload, cancellationToken);

        expense.ReceiptImageUrl = ImageUrls.ExpenseReceipt(expense.Id);
        _unitOfWork.Expenses.Update(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImageUrlResponse { Url = expense.ReceiptImageUrl };
    }

    public async Task<ImageFile> GetExpenseReceiptAsync(
        Guid expenseId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var expense = await GetOwnExpenseAsync(expenseId, currentUserId, cancellationToken);

        return await _images.ReadAsync(StorageKeys.ExpenseReceipt(expense.UserId, expense.Id), cancellationToken)
            ?? throw new NotFoundException("Трошак нема слику рачуна.");
    }

    public async Task DeleteExpenseReceiptAsync(
        Guid expenseId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var expense = await GetOwnExpenseAsync(expenseId, currentUserId, cancellationToken);

        if (expense.ReceiptImageUrl is not null)
        {
            expense.ReceiptImageUrl = null;
            _unitOfWork.Expenses.Update(expense);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Also runs when the link is already empty, so a leftover file is cleaned up too.
        await _images.DeleteAsync(StorageKeys.ExpenseReceipt(expense.UserId, expense.Id), cancellationToken);
    }

    // ---------- group expenses ----------

    public async Task<ImageUrlResponse> UploadGroupExpenseReceiptAsync(
        Guid groupId, Guid groupExpenseId, Guid currentUserId, ImageUpload upload,
        CancellationToken cancellationToken = default)
    {
        var (group, expense) = await LoadGroupExpenseAsync(groupId, groupExpenseId, cancellationToken);
        EnsureCanChangeReceipt(group, expense, currentUserId);

        await _images.SaveAsync(StorageKeys.GroupExpenseReceipt(groupId, expense.Id), upload, cancellationToken);

        expense.ReceiptImageUrl = ImageUrls.GroupExpenseReceipt(groupId, expense.Id);
        _unitOfWork.GroupExpenses.Update(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ImageUrlResponse { Url = expense.ReceiptImageUrl };
    }

    public async Task<ImageFile> GetGroupExpenseReceiptAsync(
        Guid groupId, Guid groupExpenseId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        // Every member may see the receipt - it is the proof behind a shared expense.
        if (!await _unitOfWork.GroupMembers.IsMemberAsync(groupId, currentUserId, cancellationToken))
            throw new ForbiddenException("Само члан групе може видјети рачуне групе.");

        var expense = await _unitOfWork.GroupExpenses.GetByIdAsync(groupExpenseId, cancellationToken);
        if (expense is null || expense.GroupId != groupId)
            throw new NotFoundException("Групни трошак није пронађен.");

        return await _images.ReadAsync(StorageKeys.GroupExpenseReceipt(groupId, expense.Id), cancellationToken)
            ?? throw new NotFoundException("Трошак нема слику рачуна.");
    }

    public async Task DeleteGroupExpenseReceiptAsync(
        Guid groupId, Guid groupExpenseId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var (group, expense) = await LoadGroupExpenseAsync(groupId, groupExpenseId, cancellationToken);
        EnsureCanChangeReceipt(group, expense, currentUserId);

        if (expense.ReceiptImageUrl is not null)
        {
            expense.ReceiptImageUrl = null;
            _unitOfWork.GroupExpenses.Update(expense);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await _images.DeleteAsync(StorageKeys.GroupExpenseReceipt(groupId, expense.Id), cancellationToken);
    }

    // ---------- helpers ----------

    private async Task<Expense> GetOwnExpenseAsync(Guid expenseId, Guid currentUserId, CancellationToken cancellationToken)
    {
        var expense = await _unitOfWork.Expenses.GetByIdAsync(expenseId, cancellationToken)
            ?? throw new NotFoundException("Трошак није пронађен.");

        // A personal expense - and its receipt - is visible to its owner only.
        if (expense.UserId != currentUserId)
            throw new ForbiddenException("Немате приступ овом трошку.");

        return expense;
    }

    private async Task<(Group Group, GroupExpense Expense)> LoadGroupExpenseAsync(
        Guid groupId, Guid groupExpenseId, CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Groups.GetWithMembersAsync(groupId, cancellationToken)
            ?? throw new NotFoundException("Група није пронађена.");

        var expense = await _unitOfWork.GroupExpenses.GetByIdAsync(groupExpenseId, cancellationToken);

        // An expense of another group is reported like a missing one.
        if (expense is null || expense.GroupId != groupId)
            throw new NotFoundException("Групни трошак није пронађен.");

        return (group, expense);
    }

    /// <summary>Same people who may edit the expense: the member who paid it, or the group owner.</summary>
    private static void EnsureCanChangeReceipt(Group group, GroupExpense expense, Guid currentUserId)
    {
        var membership = group.Members.FirstOrDefault(m => m.UserId == currentUserId)
            ?? throw new ForbiddenException("Нисте члан ове групе.");

        var isPayer = expense.PaidByUserId == currentUserId;
        var isOwner = membership.Role == GroupRole.Owner;
        if (!isPayer && !isOwner)
            throw new ForbiddenException("Слику рачуна може додати или уклонити само члан који је платио трошак или власник групе.");
    }
}
