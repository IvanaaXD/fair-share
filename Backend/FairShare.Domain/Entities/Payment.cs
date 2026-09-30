using FairShare.Domain.Entities.Enums;

namespace FairShare.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid SettlementTransactionId { get; set; }
    public SettlementTransaction SettlementTransaction { get; set; } = null!;

    public Guid PaymentCardId { get; set; }
    public PaymentCard PaymentCard { get; set; } = null!;

    // Спречава дуплирано процесирање истог захтјева ка Payment Simulator-у
    public string IdempotencyKey { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
