using FairShare.Domain.Entities.Enums;
using FairShare.Domain.Interfaces;
using FairShare.Domain.Services;

namespace FairShare.Application.Services;

/// <summary>
/// Balance and settlement helpers shared by GroupService, GroupExpenseService and
/// SettlementService, so the rules are written once.
/// </summary>
public static class GroupLedgerExtensions
{
    /// <summary>Loads the group's expenses and completed settlements and calculates net balances.</summary>
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
