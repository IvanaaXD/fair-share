using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Entities;

/// <summary>
/// Card payment of one settlement. A settlement has at most one payment row: a failed attempt
/// is reused by the next attempt, so the history shows the last attempt and its result.
/// </summary>
public class Payment : BaseEntity
{
    public Guid SettlementTransactionId { get; set; }
    public SettlementTransaction SettlementTransaction { get; set; } = null!;

    public Guid PaymentCardId { get; set; }
    public PaymentCard PaymentCard { get; set; } = null!;

    // Prevents duplicate processing of the same request to the Payment Simulator
    public string IdempotencyKey { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>Why the simulator declined the card (e.g. "insufficient_funds"); null on success.</summary>
    public string? FailureReason { get; set; }

    // NEW: the simulator's id of the charge ("ch_..."); null until the simulator accepts it.
    // A webhook is applied only to the attempt whose charge it reports.
    public string? ProviderChargeId { get; set; }

    // NEW: when the current attempt was sent to the simulator, and when its result arrived.
    public DateTime SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // NEW: optimistic concurrency (PostgreSQL "xmin"). The webhook and the status check may try
    // to complete the same payment at the same moment - only the first one succeeds.
    public uint Version { get; set; }
}
