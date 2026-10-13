using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.DTOs.Payments;

public class PaymentResponse
{
    public Guid Id { get; set; }
    public Guid SettlementTransactionId { get; set; }
    public PaymentStatus Status { get; set; }

    /// <summary>card_declined, insufficient_funds, expired_card, processing_error... (null on success).</summary>
    public string? FailureReason { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string CardBrand { get; set; } = string.Empty;
    public string CardLast4Digits { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
