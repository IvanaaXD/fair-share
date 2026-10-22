using System.Runtime.CompilerServices;
using FairShare.Application.Abstractions;
using FairShare.Application.Common.Caching;
using FairShare.Domain.Entities;
using FairShare.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FairShare.Infrastructure.Caching;

/// <summary>
/// Keeps the cache correct without any service having to remember it: whenever SaveChanges
/// writes an expense, a category, a user or anything that changes group balances, the cache
/// entries built from that data are invalidated.
/// This also covers writes that are easy to forget - copies of recurring expenses created by the
/// background job, blocking a user from the admin panel, a password change.
///
/// Invalidation happens only AFTER the database has saved the change. Doing it before would leave
/// a short window in which another request reloads the OLD data into the cache.
/// Same structure as FileCleanupInterceptor: one shared instance, pending tags kept per DbContext.
/// </summary>
public sealed class CacheInvalidationInterceptor : SaveChangesInterceptor
{
    private readonly ConditionalWeakTable<DbContext, HashSet<string>> _pending = new();
    private readonly ICacheService _cache;
    private readonly ILogger<CacheInvalidationInterceptor> _logger;

    public CacheInvalidationInterceptor(ICacheService cache, ILogger<CacheInvalidationInterceptor> logger)
    {
        _cache = cache;
        _logger = logger;
    }

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

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        InvalidatePendingAsync(eventData.Context).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        await InvalidatePendingAsync(eventData.Context);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

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

    private void Collect(DbContext? context)
    {
        if (context is null)
            return;

        var tags = new HashSet<string>();

        // Materialized once: the split case below looks through the same entries.
        var entries = context.ChangeTracker.Entries().ToList();

        foreach (var entry in entries)
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            switch (entry.Entity)
            {
                case Expense expense:
                    tags.Add(CacheTags.UserSpending(expense.UserId));
                    break;

                // Group balances: every group expense and split counts, ...
                case GroupExpense groupExpense:
                    tags.Add(CacheTags.GroupLedger(groupExpense.GroupId));
                    break;

                case ExpenseSplit split:
                    if (FindGroupOfSplit(split, entries) is { } splitGroupId)
                        tags.Add(CacheTags.GroupLedger(splitGroupId));
                    break;

                // ... but of the settlements only completed ones. Generating or deleting
                // suggestions (Proposed) happens often and leaves the balances as they are.
                case SettlementTransaction settlement when IsOrWasSettled(entry, settlement):
                    tags.Add(CacheTags.GroupLedger(settlement.GroupId));
                    break;

                case Category:
                    tags.Add(CacheTags.Categories);
                    break;

                case User user:
                    tags.Add(CacheTags.User(user.Id));
                    break;
            }
        }

        if (tags.Count > 0)
            _pending.AddOrUpdate(context, tags);
        else
            _pending.Remove(context);
    }

    /// <summary>
    /// A split changed on its own (e.g. only the shares were edited) still belongs to a group.
    /// The group is read from the loaded expense - the service always loads the expense together
    /// with its splits, so no extra query is needed.
    /// </summary>
    private static Guid? FindGroupOfSplit(ExpenseSplit split, IEnumerable<EntityEntry> entries)
    {
        if (split.GroupExpense is { } loaded)
            return loaded.GroupId;

        return entries
            .Select(e => e.Entity)
            .OfType<GroupExpense>()
            .FirstOrDefault(e => e.Id == split.GroupExpenseId)
            ?.GroupId;
    }

    /// <summary>True when the settlement is completed now or was completed before this save.</summary>
    private static bool IsOrWasSettled(EntityEntry entry, SettlementTransaction settlement)
        => settlement.Status == SettlementStatus.Settled
           || (entry.State == EntityState.Modified
               && entry.Property(nameof(SettlementTransaction.Status)).OriginalValue is SettlementStatus.Settled);

    private async Task InvalidatePendingAsync(DbContext? context)
    {
        if (context is null || !_pending.TryGetValue(context, out var tags))
            return;

        _pending.Remove(context);

        try
        {
            // No cancellation token: the data is already saved, the cache must follow.
            await _cache.InvalidateTagsAsync(tags);
        }
        catch (Exception ex)
        {
            // The entries still expire on their own (CacheDurations); the request is not failed.
            _logger.LogError(ex, "Cache invalidation failed for tags {Tags}.", string.Join(", ", tags));
        }
    }

    private void Discard(DbContext? context)
    {
        if (context is not null)
            _pending.Remove(context);
    }
}
