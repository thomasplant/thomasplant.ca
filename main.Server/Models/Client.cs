namespace main.Server.Models;

/// <summary>
/// A person a gallery is delivered to. Used later for share links, favorites,
/// and (optionally) email notifications.
/// </summary>
public class Client
{
    public int Id { get; set; }

    public required string Name { get; set; }
    public required string Email { get; set; }

    // Foreign key to the gallery this client was given access to.
    public int GalleryId { get; set; }

    // Navigation back to the parent gallery.
    public Gallery? Gallery { get; set; }
}
