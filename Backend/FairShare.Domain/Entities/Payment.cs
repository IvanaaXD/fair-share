using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid SettlementTransactionId { get; set; }
    public SettlementTransaction SettlementTransaction { get; set; } = null!;

    public Guid PaymentCardId { get; set; }
    public PaymentCard PaymentCard { get; set; } = null!;

    // Prevents duplicate processing of the same request to the Payment Simulator
    public string IdempotencyKey { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
