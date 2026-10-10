namespace FairShare.Application.DTOs.Files;

/// <summary>
/// An uploaded image as the controllers pass it to the services. The Application layer works
/// with a plain Stream, so it does not depend on ASP.NET Core's IFormFile.
/// </summary>
/// <param name="Content">The file content; owned (and disposed) by the caller.</param>
/// <param name="Length">Size reported by the client; the real size is checked while reading.</param>
public sealed record ImageUpload(Stream Content, long Length);

/// <summary>An image read from storage, ready to be sent to the client. Whoever returns it disposes Content.</summary>
public sealed record ImageFile(Stream Content, string ContentType);

/// <summary>Response to a receipt upload: the address from which the image can be downloaded.</summary>
public class ImageUrlResponse
{
    public string Url { get; set; } = string.Empty;
}
