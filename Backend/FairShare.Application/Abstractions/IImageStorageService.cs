using FairShare.Application.DTOs.Files;

namespace FairShare.Application.Interfaces;

/// <summary>Validates images and stores them; shared by profile images and receipts.</summary>
public interface IImageStorageService
{
    /// <summary>Checks size and format and saves the image under the key (replacing an existing one).</summary>
    Task SaveAsync(string key, ImageUpload upload, CancellationToken cancellationToken = default);

    /// <summary>Returns the image with its content type, or null if there is none under the key.</summary>
    Task<ImageFile?> ReadAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
