namespace FairShare.Application.Abstractions;

/// <summary>Претвара текст у PNG слику QR кода; имплементација (библиотека QRCoder) је у Infrastructure слоју.</summary>
public interface IQrCodeImageGenerator
{
    byte[] GeneratePng(string text, int pixelsPerModule = 10);
}
