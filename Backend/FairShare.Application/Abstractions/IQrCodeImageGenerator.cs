namespace FairShare.Application.Abstractions;

/// <summary>Converts text into a PNG image of a QR code; the implementation (QRCoder library) is in the Infrastructure layer.</summary>
public interface IQrCodeImageGenerator
{
    byte[] GeneratePng(string text, int pixelsPerModule = 10);
}
