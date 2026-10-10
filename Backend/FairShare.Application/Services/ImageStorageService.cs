using FairShare.Application.Abstractions;
using FairShare.Application.Common.Exceptions;
using FairShare.Application.Common.Files;
using FairShare.Application.DTOs.Files;
using FairShare.Application.Interfaces;

namespace FairShare.Application.Services;

public class ImageStorageService : IImageStorageService
{
    private const int CopyBufferSize = 81920;

    private readonly IFileStorage _storage;

    public ImageStorageService(IFileStorage storage)
    {
        _storage = storage;
    }

    public async Task SaveAsync(string key, ImageUpload upload, CancellationToken cancellationToken = default)
    {
        if (upload.Length <= 0)
            throw new BadRequestException("Изабрани фајл је празан.");

        if (upload.Length > ImageFileRules.MaxBytes)
            throw TooLarge();

        // The image is read into memory first (at most 5 MB) and checked there, so nothing
        // reaches the storage before it is known to be a valid image of an allowed size.
        using var buffer = new MemoryStream(capacity: (int)upload.Length);
        await CopyWithLimitAsync(upload.Content, buffer, cancellationToken);

        if (buffer.Length == 0)
            throw new BadRequestException("Изабрани фајл је празан.");

        if (!IsSupportedImage(buffer))
            throw new BadRequestException("Дозвољене су само слике у формату JPEG, PNG или WebP.");

        buffer.Position = 0;
        await _storage.SaveAsync(key, buffer, cancellationToken);
    }

    public async Task<ImageFile?> ReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var stream = await _storage.OpenReadAsync(key, cancellationToken);
        if (stream is null)
            return null;

        try
        {
            // A stream from a remote store may not support seeking; images are small, so it is
            // simply copied into memory in that case.
            if (!stream.CanSeek)
            {
                var copy = new MemoryStream();
                await stream.CopyToAsync(copy, cancellationToken);
                await stream.DisposeAsync();
                copy.Position = 0;
                stream = copy;
            }

            // The content type is recognised again from the stored bytes, so it does not have to
            // be kept anywhere (no extension in the key, no extra database column).
            var header = new byte[ImageFileRules.HeaderLength];
            var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
            stream.Position = 0;

            var contentType = ImageFileRules.DetectContentType(header.AsSpan(0, read)) ?? "application/octet-stream";
            return new ImageFile(stream, contentType);
        }
        catch
        {
            await stream.DisposeAsync();
            throw;
        }
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => _storage.DeleteAsync(key, cancellationToken);

    /// <summary>
    /// Copies the upload while counting the bytes. The size reported by the client is only a
    /// claim; this is what actually stops a larger file.
    /// </summary>
    private static async Task CopyWithLimitAsync(Stream source, Stream destination, CancellationToken cancellationToken)
    {
        var chunk = new byte[CopyBufferSize];
        long total = 0;
        int read;

        while ((read = await source.ReadAsync(chunk.AsMemory(), cancellationToken)) > 0)
        {
            total += read;
            if (total > ImageFileRules.MaxBytes)
                throw TooLarge();

            await destination.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }
    }

    private static bool IsSupportedImage(MemoryStream buffer)
    {
        var headerLength = (int)Math.Min(buffer.Length, ImageFileRules.HeaderLength);
        return ImageFileRules.DetectContentType(buffer.GetBuffer().AsSpan(0, headerLength)) is not null;
    }

    private static BadRequestException TooLarge()
        => new($"Слика може имати највише {ImageFileRules.MaxMegabytes} MB.");
}
