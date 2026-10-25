using FairShare.Application.Abstractions;
using FairShare.Application.DTOs.Files;
using FairShare.Application.Interfaces;
using FairShare.Application.Services;
using FairShare.Infrastructure.Storage;
using Microsoft.AspNetCore.Mvc;

namespace FairShare.WebAPI.Extensions
{
    public static class FileStorageServiceExtensions
    {
        /// <summary>
        /// Uploaded images (profile images, receipt photos): on the local disk by default, or in an
        /// S3-compatible object storage (SeaweedFS in Docker) when "FileStorage:Provider" is "S3". Only the
        /// IFileStorage registration differs - the services work with keys and do not know which one is used.
        /// </summary>
        public static IServiceCollection AddFileStorage(
            this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
        {
            services.Configure<FileStorageSettings>(config.GetSection("FileStorage"));

            // A relative path is resolved against the API project folder (ContentRootPath), not the
            // current directory, so files land in the same place however the API is started.
            // Path.Combine returns an absolute RootPath (e.g. /app/uploads in Docker) unchanged.
            services.PostConfigure<FileStorageSettings>(settings =>
                settings.RootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, settings.RootPath)));

            var settings = config.GetSection("FileStorage").Get<FileStorageSettings>() ?? new FileStorageSettings();
            if (settings.UsesS3)
                services.AddSingleton<IFileStorage, S3FileStorage>();
            else
                services.AddSingleton<IFileStorage, LocalFileStorage>();

            // Used by the DbContext registration in Program.cs.
            services.AddSingleton<FileCleanupInterceptor>();

            services.AddScoped<IImageStorageService, ImageStorageService>();
            services.AddScoped<IReceiptService, ReceiptService>();

            return services;
        }
    }

    public static class ImageResultExtensions
    {
        /// <summary>
        /// Returns a stored image. The content type comes from the file's own bytes, and nosniff
        /// stops the browser from guessing anything else. "private" means the image may be cached
        /// only in the user's own browser, never in a shared proxy cache.
        /// </summary>
        public static FileStreamResult ImageResult(this ControllerBase controller, ImageFile image)
        {
            controller.Response.Headers.CacheControl = "private, max-age=3600";
            controller.Response.Headers.XContentTypeOptions = "nosniff";
            return new FileStreamResult(image.Content, image.ContentType);
        }
    }
}
