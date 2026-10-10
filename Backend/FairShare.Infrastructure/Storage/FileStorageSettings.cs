namespace FairShare.Infrastructure.Storage;

/// <summary>Filled from the "FileStorage" section of appsettings.json.</summary>
public class FileStorageSettings
{
    /// <summary>
    /// Folder for uploaded files. A relative path is resolved against the API project folder
    /// (FairShare.API/uploads by default); in Docker it is the absolute path /app/uploads.
    /// </summary>
    public string RootPath { get; set; } = "uploads";
}
