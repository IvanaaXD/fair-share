using FairShare.Domain.Entities.Enums;

namespace FairShare.Application.DTOs.Qr;

public class ParsedQrResponse
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();

    public string? RecipientAccount { get; set; }
    public string? RecipientName { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? PaymentCode { get; set; }
    public string? Purpose { get; set; }
    public string? Reference { get; set; }

    /// <summary>The settlement from the system to which the QR refers (if the current user is allowed to see it).</summary>
    public Guid? MatchedSettlementId { get; set; }
    public Guid? MatchedGroupId { get; set; }
    public SettlementStatus? MatchedSettlementStatus { get; set; }

    /// <summary>true if the QR matches an outstanding settlement - the frontend then offers a "Mark as settled" button.</summary>
    public bool CanMarkAsSettled { get; set; }
}
