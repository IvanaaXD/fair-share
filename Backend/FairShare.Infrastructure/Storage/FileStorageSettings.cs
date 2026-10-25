namespace FairShare.Infrastructure.Storage;

/// <summary>Filled from the "FileStorage" section of appsettings.json.</summary>
public class FileStorageSettings
{
    /// <summary>
    /// "Local" (default): files on the server's disk, in <see cref="RootPath"/>.
    /// "S3": an S3-compatible object storage such as SeaweedFS (used in docker-compose.yml).
    /// </summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Folder for uploaded files when <see cref="Provider"/> is "Local". A relative path is
    /// resolved against the API project folder (FairShare.API/uploads by default).
    /// </summary>
    public string RootPath { get; set; } = "uploads";

    /// <summary>Used when <see cref="Provider"/> is "S3".</summary>
    public S3StorageSettings S3 { get; set; } = new();

    public bool UsesS3 => string.Equals(Provider, "S3", StringComparison.OrdinalIgnoreCase);
}

/// <summary>The "FileStorage:S3" section: where the object storage is and how to sign in.</summary>
public class S3StorageSettings
{
    /// <summary>Address of the storage, e.g. http://storage:8333 inside the Docker network.</summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>Created automatically on the first upload if it does not exist.</summary>
    public string BucketName { get; set; } = "fairshare";

    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>A self-hosted storage ignores the region, but the S3 request signature needs one.</summary>
    public string Region { get; set; } = "us-east-1";
}
