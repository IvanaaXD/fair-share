using FairShare.Application.Abstractions;
using FairShare.Application.Common.Caching;
using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Services;

namespace FairShare.Application.Common.Extensions;

/// <summary>
/// Balance and settlement helpers shared by GroupService, GroupExpenseService and
/// SettlementService, so the rules are written once.
/// </summary>
public static class GroupLedgerExtensions
{
    /// <summary>
    /// Loads the group's expenses and completed settlements and calculates net balances.
    /// Always reads the database - used wherever the result decides whether data may be
    /// changed (leaving or deleting a group, removing a member, new settlement suggestions).
    /// </summary>
    public static async Task<Dictionary<Guid, decimal>> GetGroupNetBalancesAsync(
        this IUnitOfWork unitOfWork,
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        var expenses = await unitOfWork.GroupExpenses.GetAllByGroupAsync(groupId, cancellationToken);
        var settled = await unitOfWork.SettlementTransactions.GetByGroupAsync(
            groupId, SettlementStatus.Settled, cancellationToken);

        return GroupBalanceCalculator.Calculate(expenses, settled);
    }

    /// <summary>
    /// The same balances, from the cache when possible - for DISPLAYING them only.
    /// Calculating balances loads every expense of the group, so it is the slowest read in the
    /// app (see the load tests). The entry is invalidated by CacheInvalidationInterceptor as soon
    /// as an expense, a split or a completed settlement of the group is saved.
    /// Only the numbers are cached, never the members: the caller checks membership itself.
    /// </summary>
    public static Task<Dictionary<Guid, decimal>> GetCachedGroupNetBalancesAsync(
        this IUnitOfWork unitOfWork,
        ICacheService cache,
        Guid groupId,
        CancellationToken cancellationToken = default)
        => cache.GetOrCreateAsync(
            CacheKeys.GroupBalances(groupId),
            token => unitOfWork.GetGroupNetBalancesAsync(groupId, token),
            CacheDurations.GroupBalances,
            new[] { CacheTags.GroupLedger(groupId) },
            cancellationToken);

    /// <summary>
    /// Marks still-unsettled settlement suggestions for deletion. Suggestions are a snapshot of
    /// "who owes whom" at the moment they were generated; once an expense or the membership
    /// changes they are out of date and must not be paid. Completed settlements are never touched.
    /// The caller saves the changes, so the deletion happens in the same transaction as the
    /// change that caused it.
    /// </summary>
    /// <param name="involvingUserId">If set, only suggestions where this user is debtor or creditor.</param>
    public static async Task RemoveProposedSettlementsAsync(
        this IUnitOfWork unitOfWork,
        Guid groupId,
        Guid? involvingUserId = null,
        CancellationToken cancellationToken = default)
    {
        var proposed = await unitOfWork.SettlementTransactions.GetForUpdateByGroupAsync(
            groupId, SettlementStatus.Proposed, cancellationToken);

        foreach (var settlement in proposed)
        {
            if (involvingUserId is null ||
                settlement.DebtorUserId == involvingUserId ||
                settlement.CreditorUserId == involvingUserId)
            {
                unitOfWork.SettlementTransactions.Remove(settlement);
            }
        }
    }
}
