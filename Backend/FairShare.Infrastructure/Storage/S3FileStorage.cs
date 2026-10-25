using System.Net;
using System.Text.RegularExpressions;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FairShare.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace FairShare.Infrastructure.Storage;

/// <summary>
/// Stores files in an S3-compatible object storage (SeaweedFS in docker-compose.yml). The keys are
/// the same as on the local disk ("receipts/users/{userId}/{expenseId}"), so the services do not
/// know which storage is used. The bucket is private: every file is still read through an API
/// endpoint that first checks who is asking.
/// </summary>
public sealed class S3FileStorage : IFileStorage, IDisposable
{
    // Same rule as LocalFileStorage: keys are built from ids only.
    private static readonly Regex KeyPattern = new("^[a-z0-9-]+(/[a-z0-9-]+)*$", RegexOptions.Compiled);

    // DeleteObjects accepts at most 1000 keys per request.
    private const int MaxKeysPerDelete = 1000;

    private readonly AmazonS3Client _client;
    private readonly string _bucket;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private volatile bool _bucketReady;

    public S3FileStorage(IOptions<FileStorageSettings> settings)
    {
        var s3 = settings.Value.S3;
        if (string.IsNullOrWhiteSpace(s3.ServiceUrl))
            throw new InvalidOperationException("FileStorage:S3:ServiceUrl must be set when FileStorage:Provider is \"S3\".");

        _bucket = s3.BucketName;
        var config = new AmazonS3Config
        {
            ServiceURL = s3.ServiceUrl,
            AuthenticationRegion = s3.Region,
            // A self-hosted storage addresses buckets by path (http://storage:8333/fairshare/key), not by subdomain.
            ForcePathStyle = true,
            // Checksums only where S3 requires them; self-hosted storages may not support the newer optional ones.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
        };
        _client = new AmazonS3Client(new BasicAWSCredentials(s3.AccessKey, s3.SecretKey), config);
    }

    public async Task SaveAsync(string key, Stream content, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        await EnsureBucketAsync(cancellationToken);

        // S3 needs the length up front; images are at most a few MB, so a copy in memory is fine.
        // A PUT replaces the whole object at once, so a reader never sees a half-written image.
        var body = content;
        if (!content.CanSeek)
        {
            body = new MemoryStream();
            await content.CopyToAsync(body, cancellationToken);
            body.Position = 0;
        }

        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = body,
            AutoCloseStream = !ReferenceEquals(body, content),
            ContentType = "application/octet-stream",
            UseChunkEncoding = false
        }, cancellationToken);
    }

    public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        await EnsureBucketAsync(cancellationToken);

        try
        {
            using var response = await _client.GetObjectAsync(_bucket, key, cancellationToken);

            // Copied into memory: the caller gets an ordinary seekable stream (the image type is
            // read from the first bytes) and the HTTP connection to the storage is released at once.
            var copy = new MemoryStream();
            await response.ResponseStream.CopyToAsync(copy, cancellationToken);
            copy.Position = 0;
            return copy;
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        await EnsureBucketAsync(cancellationToken);

        // Deleting an object that does not exist is not an error in S3 either.
        await _client.DeleteObjectAsync(_bucket, key, cancellationToken);
    }

    public async Task DeleteFolderAsync(string folderKey, CancellationToken cancellationToken = default)
    {
        ValidateKey(folderKey);
        await EnsureBucketAsync(cancellationToken);

        // S3 has no folders: everything whose key starts with "folder/" is listed and deleted.
        var request = new ListObjectsV2Request { BucketName = _bucket, Prefix = folderKey + "/" };
        ListObjectsV2Response response;
        do
        {
            response = await _client.ListObjectsV2Async(request, cancellationToken);
            var keys = (response.S3Objects ?? new List<S3Object>()).Select(o => o.Key).ToList();

            foreach (var batch in keys.Chunk(MaxKeysPerDelete))
            {
                await _client.DeleteObjectsAsync(new DeleteObjectsRequest
                {
                    BucketName = _bucket,
                    Objects = batch.Select(k => new KeyVersion { Key = k }).ToList()
                }, cancellationToken);
            }

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (response.IsTruncated == true);
    }

    public void Dispose()
    {
        _client.Dispose();
        _bucketLock.Dispose();
    }

    /// <summary>Creates the bucket on first use, so a fresh storage needs no manual setup.</summary>
    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketReady)
            return;

        await _bucketLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketReady)
                return;

            try
            {
                await _client.PutBucketAsync(new PutBucketRequest { BucketName = _bucket }, cancellationToken);
            }
            catch (AmazonS3Exception e) when (e.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
            {
                // Already there (created on an earlier start).
            }

            _bucketReady = true;
        }
        finally
        {
            _bucketLock.Release();
        }
    }

    private static void ValidateKey(string key)
    {
        if (!KeyPattern.IsMatch(key))
            throw new ArgumentException($"Invalid storage key '{key}'.", nameof(key));
    }
}
