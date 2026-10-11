namespace FairShare.Application.DTOs.Qr;

/// <summary>QR code text and the fields it is composed of (for display on the frontend).</summary>
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
