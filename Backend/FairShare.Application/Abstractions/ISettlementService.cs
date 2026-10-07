using FairShare.Application.DTOs.Settlements;
using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.Interfaces;

public interface ISettlementService
{
    /// <summary>Net balance of every member of the group. Members only.</summary>
    Task<IReadOnlyList<BalanceResponse>> GetGroupBalancesAsync(
        Guid groupId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates balances and generates a new settlement suggestion, replacing earlier
    /// unsettled suggestions. useExactAlgorithm selects the exact algorithm instead of the
    /// greedy one (small groups only). Members only.
    /// </summary>
    Task<IReadOnlyList<SettlementTransactionResponse>> GenerateSettlementSuggestionsAsync(
        Guid groupId,
        bool useExactAlgorithm,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Marks a settlement as paid (e.g. in cash). Only its debtor or creditor may do it.</summary>
    Task<SettlementTransactionResponse> MarkAsSettledAsync(
        Guid groupId,
        Guid settlementTransactionId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Settlements of the group, optionally filtered by status. Members only.</summary>
    Task<IReadOnlyList<SettlementTransactionResponse>> GetGroupSettlementsAsync(
        Guid groupId,
        SettlementStatus? status,
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}
