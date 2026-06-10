using main.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace main.Server.Data;

/// <summary>
/// The Entity Framework Core context for the app. One instance lives for the
/// duration of a single HTTP request (registered as a scoped service in
/// Program.cs) and is the gateway for all database reads and writes.
/// </summary>
public class AppDbContext : DbContext
{
    // EF needs to pass options (which provider, which connection string) into
    // the base DbContext. DI supplies these — see AddDbContext in Program.cs.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Each DbSet<T> is a table. Querying Galleries translates to SQL against
    // the "Galleries" table; adding to it and calling SaveChanges INSERTs rows.
    public DbSet<Gallery> Galleries => Set<Gallery>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<Client> Clients => Set<Client>();

    // Configuration the naming/type conventions can't infer on their own.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Gallery>(gallery =>
        {
            // Slugs are public URLs -> must be unique across all galleries.
            gallery.HasIndex(g => g.Slug).IsUnique();

            // Deleting a gallery deletes its photos too (DB-level ON DELETE CASCADE).
            gallery.HasMany(g => g.Photos)
                   .WithOne(p => p.Gallery)
                   .HasForeignKey(p => p.GalleryId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Clients are NOT auto-deleted with the gallery — we want to decide
            // that explicitly. Restrict makes the DB reject an orphaning delete.
            gallery.HasMany(g => g.Clients)
                   .WithOne(c => c.Gallery)
                   .HasForeignKey(c => c.GalleryId)
                   .OnDelete(DeleteBehavior.Restrict);
        });
    }

    // Centralize timestamp defaults so callers never have to remember them, and
    // so values are always UTC (Npgsql stores DateTime as timestamptz and
    // requires UTC). Runs on every save, just before SQL is generated.
    public override int SaveChanges()
    {
        ApplyTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Gallery>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<Photo>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.UploadedAt = now;
            }
        }
    }
}
