namespace FairShare.Application.Common.Files;

/// <summary>Which images are accepted and how they are recognised.</summary>
public static class ImageFileRules
{
    public const int MaxMegabytes = 5;

    public const long MaxBytes = MaxMegabytes * 1024L * 1024L;

    /// <summary>
    /// Limit for the whole upload request: the image plus room for the multipart envelope.
    /// Larger requests are rejected by the server before they are read into memory.
    /// </summary>
    public const long MaxRequestBytes = MaxBytes + 64 * 1024;

    /// <summary>Number of leading bytes needed to recognise every supported format.</summary>
    public const int HeaderLength = 12;

    private static ReadOnlySpan<byte> PngSignature => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>
    /// Recognises the format from the file's first bytes ("magic numbers"). The file name and
    /// the Content-Type sent by the client are ignored, because the client can set both to
    /// anything - e.g. upload an HTML page named "receipt.jpg".
    /// Returns the content type, or null if the file is not a JPEG, PNG or WebP image.
    /// </summary>
    public static string? DetectContentType(ReadOnlySpan<byte> header)
    {
        // JPEG: FF D8 FF
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            return "image/jpeg";

        // PNG: 89 'P' 'N' 'G' 0D 0A 1A 0A
        if (header.Length >= PngSignature.Length && header[..PngSignature.Length].SequenceEqual(PngSignature))
            return "image/png";

        // WebP: "RIFF" <4-byte size> "WEBP"
        if (header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8))
            return "image/webp";

        // SVG is deliberately not supported: it is XML that can contain scripts.
        return null;
    }
}
