using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace main.Server.Storage;

/// <summary>
/// IStorageService backed by the AWS S3 SDK. Works against MinIO locally and
/// real S3 in production with no code change — only the endpoint/credentials
/// in configuration differ.
/// </summary>
public class StorageService : IStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly StorageOptions _options;

    // IAmazonS3 is injected as a singleton (constructed in Program.cs); the
    // options come from the bound "Storage" config section via IOptions<T>.
    public StorageService(IAmazonS3 s3, IOptions<StorageOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public async Task EnsureBucketsExistAsync(CancellationToken cancellationToken = default)
    {
        foreach (var bucket in new[] { _options.OriginalsBucket, _options.ThumbnailsBucket })
        {
            // AmazonS3Util.DoesS3BucketExistV2Async would also work; here we
            // try-create and treat "already owned by you" as success.
            var exists = await Amazon.S3.Util.AmazonS3Util
                .DoesS3BucketExistV2Async(_s3, bucket);

            if (!exists)
            {
                await _s3.PutBucketAsync(new PutBucketRequest { BucketName = bucket },
                    cancellationToken);
            }
        }
    }

    public async Task UploadAsync(string bucket, string key, Stream content,
        string contentType, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            // Let the SDK compute Content-Length from the stream.
            AutoCloseStream = false,
        };

        await _s3.PutObjectAsync(request, cancellationToken);
    }

    public string GetPresignedUrl(string bucket, string key, TimeSpan expiry)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiry),
        };

        // Pure signing — no network call. Returns a URL with a signature in the
        // query string that storage validates until it expires.
        var url = _s3.GetPreSignedURL(request);

        // The SDK forces https on presigned URLs. Only the host is signed
        // (X-Amz-SignedHeaders=host), not the scheme, so we can safely rewrite
        // the scheme to match a plain-http endpoint like local MinIO.
        if (_options.Endpoint is not null &&
            _options.Endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = string.Concat("http://", url.AsSpan("https://".Length));
        }

        return url;
    }

    public async Task DeleteAsync(string bucket, string key,
        CancellationToken cancellationToken = default)
    {
        await _s3.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = bucket,
            Key = key,
        }, cancellationToken);
    }
}
