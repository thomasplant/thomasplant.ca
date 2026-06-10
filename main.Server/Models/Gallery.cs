namespace main.Server.Models;

/// <summary>
/// A collection of photos delivered to a client (e.g. a wedding shoot).
/// Reachable publicly by its <see cref="Slug"/>.
/// </summary>
public class Gallery
{
    // Convention: a property named "Id" is the primary key.
    // An int PK becomes a Postgres IDENTITY column (DB-generated on insert).
    public int Id { get; set; }

    // "required" = the C# compiler refuses to construct a Gallery without
    // setting these. Combined with <Nullable>enable</Nullable>, EF makes the
    // columns NOT NULL.
    public required string Title { get; set; }

    // The URL-friendly identifier used in public links: /galleries/jane-john-wedding
    public required string Slug { get; set; }

    // Nullable: a gallery may not have a cover chosen yet -> NULL column.
    public string? CoverImageUrl { get; set; }

    // Timestamps. Defaults are set in AppDbContext so every row gets UtcNow
    // without each caller remembering to. Npgsql stores these as timestamptz
    // and requires UTC values.
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation property: the "many" side of one-Gallery-has-many-Photos.
    // EF sees this collection + Photo.GalleryId and builds the relationship.
    // Initialized to an empty list so it's never null in memory.
    public List<Photo> Photos { get; set; } = [];

    // A gallery can be delivered to one or more clients.
    public List<Client> Clients { get; set; } = [];
}
