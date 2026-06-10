namespace main.Server.Models;

/// <summary>
/// A single image within a <see cref="Gallery"/>. The database stores only
/// metadata and the object-storage keys — the actual image bytes live in
/// MinIO/S3 and are served via presigned URLs (Phase 2).
/// </summary>
public class Photo
{
    public int Id { get; set; }

    // Foreign key. The "GalleryId + Gallery navigation" pair is the convention
    // that tells EF this Photo belongs to exactly one Gallery.
    public int GalleryId { get; set; }

    // The object-storage keys (paths) for the full-res original and the
    // generated thumbnail. We never store the image bytes in Postgres.
    public required string OriginalKey { get; set; }
    public required string ThumbnailKey { get; set; }

    // Intrinsic pixel dimensions — handy for masonry layout / aspect ratios
    // on the frontend without downloading the image first.
    public int Width { get; set; }
    public int Height { get; set; }

    // Manual ordering within the gallery (drag-and-drop reorder in Phase 5).
    public int SortOrder { get; set; }

    public DateTime UploadedAt { get; set; }

    // Navigation back to the parent. Nullable because when we load a Photo on
    // its own we may not have asked EF to also load its Gallery.
    public Gallery? Gallery { get; set; }
}
