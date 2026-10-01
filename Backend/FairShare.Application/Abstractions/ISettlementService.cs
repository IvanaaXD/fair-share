using FairShare.Application.DTOs.Settlements;
using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.Interfaces;

public interface ISettlementService
{
    /// <summary>Нето салдо сваког члана групе (функционалност 5.7).</summary>
    Task<IReadOnlyList<BalanceResponse>> GetGroupBalancesAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Прерачунава салда и генерише нов приједлог поравнања (замјењује претходне
    /// неизмирене приједлоге). useExactAlgorithm бира егзактни алгоритам умјесто
    /// greedy-ja - примјењиво само на мање групе.
    /// </summary>
    Task<IReadOnlyList<SettlementTransactionResponse>> GenerateSettlementSuggestionsAsync(
        Guid groupId,
        bool useExactAlgorithm,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Ручно означавање поравнања као измиреног (нпр. готовински, ван система).</summary>
    Task<SettlementTransactionResponse> MarkAsSettledAsync(
        Guid settlementTransactionId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SettlementTransactionResponse>> GetGroupSettlementsAsync(
        Guid groupId,
        SettlementStatus? status,
        CancellationToken cancellationToken = default);
}
