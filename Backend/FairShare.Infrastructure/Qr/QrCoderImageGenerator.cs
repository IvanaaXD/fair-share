using FairShare.Application.Abstractions;
using QRCoder;

namespace FairShare.Infrastructure.Qr;

/// <summary>
/// Generates a PNG image of a QR code using the QRCoder library. PngByteQRCode does not depend on
/// System.Drawing, so it works on both Windows and Linux (e.g., in a Docker container).
/// </summary>
public class QrCoderImageGenerator : IQrCodeImageGenerator
{
    public byte[] GeneratePng(string text, int pixelsPerModule = 10)
    {
        using var generator = new QRCodeGenerator();

        // ECC level M (~15% error correction) - standard choice for payment QR codes.
        // forceUtf8 is required due to Cyrillic/Latin letters with diacritics in the name.
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M, forceUtf8: true);

        return new PngByteQRCode(data).GetGraphic(pixelsPerModule);
    }
}