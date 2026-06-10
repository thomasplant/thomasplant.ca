using SkiaSharp;

namespace main.Server.Storage;

/// <summary>
/// Result of decoding/resizing an image: the encoded JPEG bytes plus the
/// pixel dimensions of the source image.
/// </summary>
public record ProcessedImage(byte[] Bytes, int Width, int Height);

/// <summary>
/// Thin wrapper over SkiaSharp for the two operations the upload flow needs:
/// reading an image's dimensions and producing a downsized thumbnail.
/// </summary>
public static class ImageProcessor
{
    /// <summary>
    /// Decode an uploaded image to learn its real pixel dimensions. Returns the
    /// original bytes unchanged alongside the width/height.
    /// </summary>
    public static ProcessedImage Probe(byte[] original)
    {
        using var bitmap = SKBitmap.Decode(original)
            ?? throw new InvalidOperationException("File is not a decodable image.");

        return new ProcessedImage(original, bitmap.Width, bitmap.Height);
    }

    /// <summary>
    /// Produce a JPEG thumbnail no larger than <paramref name="maxEdge"/> on its
    /// longest side, preserving aspect ratio. Images already smaller are encoded
    /// as-is (no upscaling).
    /// </summary>
    public static ProcessedImage CreateThumbnail(byte[] original, int maxEdge = 400, int quality = 80)
    {
        using var source = SKBitmap.Decode(original)
            ?? throw new InvalidOperationException("File is not a decodable image.");

        // Scale factor so the longest edge becomes maxEdge; never upscale (>1).
        var scale = Math.Min(1f, (float)maxEdge / Math.Max(source.Width, source.Height));
        var targetWidth = Math.Max(1, (int)(source.Width * scale));
        var targetHeight = Math.Max(1, (int)(source.Height * scale));

        var info = new SKImageInfo(targetWidth, targetHeight);
        using var resized = source.Resize(info, SKSamplingOptions.Default)
            ?? throw new InvalidOperationException("Failed to resize image.");

        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);

        return new ProcessedImage(data.ToArray(), source.Width, source.Height);
    }
}
