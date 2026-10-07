namespace FairShare.Application.DTOs.Qr;

/// <summary>Текст који је frontend прочитао из QR кода (камером или из учитане слике).</summary>
public class ParseQrRequest
{
    public string Payload { get; set; } = string.Empty;
}
