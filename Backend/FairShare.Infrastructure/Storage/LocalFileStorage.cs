using System.Text.RegularExpressions;
using FairShare.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace FairShare.Infrastructure.Storage;

/// <summary>
/// Stores files in a folder on the server's disk. The folder is outside wwwroot and is never
/// served directly: every file is read through an API endpoint that first checks who is asking.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private const int BufferSize = 81920;

    // Keys are built by StorageKeys from ids only. This check is a second line of defence: a key
    // with "..", a drive letter or any unexpected character can never escape the root folder.
    private static readonly Regex KeyPattern = new("^[a-z0-9-]+(/[a-z0-9-]+)*$", RegexOptions.Compiled);

    private readonly string _rootPath;

    public LocalFileStorage(IOptions<FileStorageSettings> settings)
    {
        _rootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(settings.Value.RootPath));
        Directory.CreateDirectory(_rootPath);
    }

    public async Task SaveAsync(string key, Stream content, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Written to a temporary file and then moved over the target in one step, so a reader
        // never sees a half-written image and a failed upload never damages the existing one.
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var file = new FileStream(
                temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
            {
                await content.CopyToAsync(file, cancellationToken);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(key);
        if (!File.Exists(path))
            return Task.FromResult<Stream?>(null);

        // FileShare.Delete lets an upload replace (or a delete remove) the file while it is being read.
        Stream stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, BufferSize, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(key);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    public Task DeleteFolderAsync(string folderKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(folderKey);
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);

        return Task.CompletedTask;
    }

    private string ResolvePath(string key)
    {
        if (!KeyPattern.IsMatch(key))
            throw new ArgumentException($"Invalid storage key '{key}'.", nameof(key));

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, key.Replace('/', Path.DirectorySeparatorChar)));
        if (!fullPath.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException($"Storage key '{key}' points outside the storage folder.", nameof(key));

        return fullPath;
    }
}
