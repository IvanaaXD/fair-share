namespace FairShare.Application.DTOs.Qr;

/// <summary>Текст QR кода и поља од којих је састављен (за приказ на frontend-у).</summary>
public class QrPayloadResponse
{
    public Guid SettlementTransactionId { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string RecipientAccount { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Purpose { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;
}
