using main.Server.Data;
using main.Server.Models;
using main.Server.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace main.Server.Controllers;

[ApiController]
[Route("api")]
public class PhotosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IStorageService _storage;
    private readonly StorageOptions _storageOptions;

    // Everything arrives by constructor injection: the DbContext (scoped, this
    // request), the storage service, and the bound StorageOptions.
    public PhotosController(
        AppDbContext db,
        IStorageService storage,
        IOptions<StorageOptions> storageOptions)
    {
        _db = db;
        _storage = storage;
        _storageOptions = storageOptions.Value;
    }

    /// <summary>
    /// Upload a photo into a gallery: store the original, generate + store a
    /// thumbnail, and record a Photo row. Returns the created photo with
    /// presigned URLs for immediate display.
    /// </summary>
    [HttpPost("galleries/{galleryId:int}/photos")]
    public async Task<IActionResult> Upload(int galleryId, IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        // The gallery must exist — otherwise we'd orphan objects in storage.
        var galleryExists = await _db.Galleries
            .AnyAsync(g => g.Id == galleryId, cancellationToken);
        if (!galleryExists)
        {
            return NotFound($"Gallery {galleryId} not found.");
        }

        // Read the upload into memory once; we need the bytes twice (store the
        // original, and decode for the thumbnail). Fine for typical photos.
        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms, cancellationToken);
            bytes = ms.ToArray();
        }

        // Decode for dimensions + build the thumbnail. CreateThumbnail returns
        // the SOURCE width/height (handy) plus the resized JPEG bytes.
        ProcessedImage thumb;
        try
        {
            thumb = ImageProcessor.CreateThumbnail(bytes);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        // Unique, collision-proof keys. We don't trust the client filename.
        var id = Guid.NewGuid().ToString("N");
        var originalKey = $"{id}.jpg";
        var thumbnailKey = $"{id}.jpg";

        // Store both objects. Originals keep their uploaded content type;
        // thumbnails are always JPEG.
        using (var originalStream = new MemoryStream(bytes))
        {
            await _storage.UploadAsync(_storageOptions.OriginalsBucket, originalKey,
                originalStream, file.ContentType, cancellationToken);
        }
        using (var thumbStream = new MemoryStream(thumb.Bytes))
        {
            await _storage.UploadAsync(_storageOptions.ThumbnailsBucket, thumbnailKey,
                thumbStream, "image/jpeg", cancellationToken);
        }

        // Record the row. UploadedAt is stamped by AppDbContext.SaveChanges.
        var photo = new Photo
        {
            GalleryId = galleryId,
            OriginalKey = originalKey,
            ThumbnailKey = thumbnailKey,
            Width = thumb.Width,
            Height = thumb.Height,
            SortOrder = 0,
        };
        _db.Photos.Add(photo);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ToDto(photo));
    }

    /// <summary>Delete a photo: remove its objects from storage and its row.</summary>
    [HttpDelete("photos/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var photo = await _db.Photos.FindAsync([id], cancellationToken);
        if (photo is null)
        {
            return NotFound();
        }

        await _storage.DeleteAsync(_storageOptions.OriginalsBucket, photo.OriginalKey, cancellationToken);
        await _storage.DeleteAsync(_storageOptions.ThumbnailsBucket, photo.ThumbnailKey, cancellationToken);

        _db.Photos.Remove(photo);
        await _db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // Map the entity to a response shape with short-lived presigned URLs the
    // browser can use to fetch the images directly from storage. We never send
    // the raw storage keys to the client.
    private PhotoDto ToDto(Photo photo)
    {
        var expiry = TimeSpan.FromMinutes(15);
        return new PhotoDto(
            photo.Id,
            photo.GalleryId,
            _storage.GetPresignedUrl(_storageOptions.OriginalsBucket, photo.OriginalKey, expiry),
            _storage.GetPresignedUrl(_storageOptions.ThumbnailsBucket, photo.ThumbnailKey, expiry),
            photo.Width,
            photo.Height,
            photo.SortOrder,
            photo.UploadedAt);
    }
}

/// <summary>
/// What the API returns for a photo. Note: presigned URLs instead of storage
/// keys, and no Gallery navigation property (avoids serialization cycles).
/// </summary>
public record PhotoDto(
    int Id,
    int GalleryId,
    string OriginalUrl,
    string ThumbnailUrl,
    int Width,
    int Height,
    int SortOrder,
    DateTime UploadedAt);
