namespace FairShare.Application.Abstractions;

/// <summary>
/// Where uploaded files are physically kept. The application only works with keys
/// (e.g. "receipts/users/{userId}/{expenseId}"), so the local-disk implementation can later be
/// replaced by an object store (S3, Azure Blob...) without touching the services.
/// </summary>
public interface IFileStorage
{
    /// <summary>Saves the content under the key, replacing an existing file with the same key.</summary>
    Task SaveAsync(string key, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Opens the file for reading, or returns null if it does not exist. The caller disposes the stream.</summary>
    Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Deletes the file. Deleting a file that does not exist is not an error.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Deletes everything under the key prefix (e.g. all receipts of a deleted group).</summary>
    Task DeleteFolderAsync(string folderKey, CancellationToken cancellationToken = default);
}
