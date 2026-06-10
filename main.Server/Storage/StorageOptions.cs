namespace main.Server.Storage;

/// <summary>
/// Strongly-typed view of the "Storage" configuration section. Bound once in
/// Program.cs from env vars (Docker: Storage__Endpoint, ...) or User Secrets
/// (local dev). Lets the rest of the app read settings without magic strings.
/// </summary>
public class StorageOptions
{
    public const string SectionName = "Storage";

    // The S3 API endpoint. MinIO locally (http://localhost:9000) or, in
    // production, left null so the SDK uses real AWS S3.
    public string? Endpoint { get; set; }

    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    // Bucket names. Defaulted so they exist even if config omits them.
    public string OriginalsBucket { get; set; } = "originals";
    public string ThumbnailsBucket { get; set; } = "thumbnails";
}
