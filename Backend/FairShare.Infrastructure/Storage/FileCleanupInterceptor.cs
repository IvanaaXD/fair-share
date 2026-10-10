using System.Runtime.CompilerServices;
using FairShare.Application.Abstractions;
using FairShare.Application.Common.Files;
using FairShare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FairShare.Infrastructure.Storage;

/// <summary>
/// Deletes the files of deleted records: the receipt of a deleted expense, all receipts of a
/// deleted group, the profile image and receipts of a deleted user.
///
/// It hooks into SaveChanges, so ExpenseService, GroupExpenseService and GroupService did not have
/// to change, and no future delete can forget its files. Files are deleted only AFTER the database
/// has saved the change - if saving fails, the records and their files both stay.
///
/// Registered once (singleton) and shared by all DbContext instances; the files waiting for a
/// particular SaveChanges are kept per context instance.
/// </summary>
public sealed class FileCleanupInterceptor : SaveChangesInterceptor
{
    private readonly ConditionalWeakTable<DbContext, List<PendingDeletion>> _pending = new();
    private readonly IFileStorage _storage;
    private readonly ILogger<FileCleanupInterceptor> _logger;

    public FileCleanupInterceptor(IFileStorage storage, ILogger<FileCleanupInterceptor> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    private sealed record PendingDeletion(string Key, bool IsFolder);

    // ---------- before saving: remember which files belong to deleted records ----------

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Collect(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Collect(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // ---------- after a successful save: delete them ----------

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DeletePendingAsync(eventData.Context).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        await DeletePendingAsync(eventData.Context);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    // ---------- failed or cancelled save: keep the files ----------

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        Discard(eventData.Context);
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Discard(eventData.Context);
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    public override void SaveChangesCanceled(DbContextEventData eventData)
    {
        Discard(eventData.Context);
        base.SaveChangesCanceled(eventData);
    }

    public override Task SaveChangesCanceledAsync(DbContextEventData eventData, CancellationToken cancellationToken = default)
    {
        Discard(eventData.Context);
        return base.SaveChangesCanceledAsync(eventData, cancellationToken);
    }

    // ---------- helpers ----------

    private void Collect(DbContext? context)
    {
        if (context is null)
            return;

        var deletions = new List<PendingDeletion>();

        foreach (var entry in context.ChangeTracker.Entries().Where(e => e.State == EntityState.Deleted))
        {
            switch (entry.Entity)
            {
                case Expense expense:
                    deletions.Add(new(StorageKeys.ExpenseReceipt(expense.UserId, expense.Id), IsFolder: false));
                    break;

                case GroupExpense groupExpense:
                    deletions.Add(new(StorageKeys.GroupExpenseReceipt(groupExpense.GroupId, groupExpense.Id), IsFolder: false));
                    break;

                // The database deletes the group's expenses by cascade without loading them, so
                // the whole folder with the group's receipts is removed instead.
                case Group group:
                    deletions.Add(new(StorageKeys.GroupReceiptsFolder(group.Id), IsFolder: true));
                    break;

                case User user:
                    deletions.Add(new(StorageKeys.ProfileImage(user.Id), IsFolder: false));
                    deletions.Add(new(StorageKeys.UserReceiptsFolder(user.Id), IsFolder: true));
                    break;
            }
        }

        if (deletions.Count > 0)
            _pending.AddOrUpdate(context, deletions);
        else
            _pending.Remove(context);
    }

    private async Task DeletePendingAsync(DbContext? context)
    {
        if (context is null || !_pending.TryGetValue(context, out var deletions))
            return;

        _pending.Remove(context);

        foreach (var deletion in deletions)
        {
            try
            {
                // No cancellation token: the records are already deleted, the files must follow.
                if (deletion.IsFolder)
                    await _storage.DeleteFolderAsync(deletion.Key);
                else
                    await _storage.DeleteAsync(deletion.Key);
            }
            catch (Exception ex)
            {
                // The request has already succeeded; a leftover file is only logged.
                _logger.LogWarning(ex, "Could not delete stored file {Key}.", deletion.Key);
            }
        }
    }

    private void Discard(DbContext? context)
    {
        if (context is not null)
            _pending.Remove(context);
    }
}
