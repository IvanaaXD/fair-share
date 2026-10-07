using FairShare.Application.Abstractions;
using QRCoder;

namespace FairShare.Infrastructure.Qr;

/// <summary>
/// Генерише PNG слику QR кода помоћу библиотеке QRCoder. PngByteQRCode не зависи од
/// System.Drawing, па ради и на Windows-у и на Linux-у (нпр. у Docker контејнеру).
/// </summary>
public class QrCoderImageGenerator : IQrCodeImageGenerator
{
    public byte[] GeneratePng(string text, int pixelsPerModule = 10)
    {
        using var generator = new QRCodeGenerator();

        // ECC ниво M (~15% корекције грешака) - стандардни избор за QR кодове плаћања.
        // forceUtf8 је потребан због ћириличних/латиничних слова са дијакритицима у имену.
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M, forceUtf8: true);

        return new PngByteQRCode(data).GetGraphic(pixelsPerModule);
    }
}
