namespace FairShare.Application.DTOs.Qr;

/// <summary>Text read by the frontend from the QR code (via camera or from an uploaded image).</summary>
public class ParseQrRequest
{
    public string Payload { get; set; } = string.Empty;
}
