namespace main.Server.Storage;

/// <summary>
/// Abstraction over object storage (MinIO locally, S3 in production). The rest
/// of the app depends on this interface, not the AWS SDK directly — so callers
/// never know or care which backend is behind it.
/// </summary>
public interface IStorageService
{
    /// <summary>Create the originals/thumbnails buckets if they don't exist.</summary>
    Task EnsureBucketsExistAsync(CancellationToken cancellationToken = default);

    /// <summary>Store a stream of bytes under <paramref name="key"/> in a bucket.</summary>
    Task UploadAsync(string bucket, string key, Stream content, string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a temporary signed URL the browser can use to fetch the object
    /// directly from storage, without the bucket being public.
    /// </summary>
    string GetPresignedUrl(string bucket, string key, TimeSpan expiry);

    /// <summary>Delete an object (e.g. when its Photo row is removed).</summary>
    Task DeleteAsync(string bucket, string key, CancellationToken cancellationToken = default);
}
