using Amazon.S3;
using main.Server.Data;
using main.Server.Storage;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Read the connection string from the layered configuration: env var in
// Docker, User Secrets in local dev. Fail loudly if neither supplied one.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found. Set it via User Secrets " +
        "(local) or the ConnectionStrings__DefaultConnection env var (Docker).");

// Register AppDbContext as a scoped service backed by the Npgsql provider.
// Controllers receive it through constructor injection — never 'new' it.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Bind the "Storage" config section to StorageOptions (env vars in Docker,
// User Secrets locally). Injected anywhere as IOptions<StorageOptions>.
builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection(StorageOptions.SectionName));

// The S3 client is thread-safe and reusable -> register ONE shared singleton,
// configured to point at MinIO (or real S3 in prod).
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var storage = builder.Configuration
        .GetSection(StorageOptions.SectionName)
        .Get<StorageOptions>() ?? new StorageOptions();

    var config = new AmazonS3Config
    {
        // Path-style URLs (host:9000/bucket/key) — required for MinIO, since
        // bucket-as-subdomain doesn't resolve on localhost.
        ForcePathStyle = true,
        // The SDK demands a region even though MinIO ignores it.
        AuthenticationRegion = "us-east-1",
    };

    // When an endpoint is configured (MinIO), point the client at it. Left
    // null in production so the SDK uses the real AWS S3 endpoints.
    if (!string.IsNullOrWhiteSpace(storage.Endpoint))
    {
        config.ServiceURL = storage.Endpoint;
    }

    return new AmazonS3Client(storage.AccessKey, storage.SecretKey, config);
});

// The storage service itself: scoped is fine since it's a thin wrapper.
builder.Services.AddScoped<IStorageService, StorageService>();

var app = builder.Build();

// Ensure the storage buckets exist before serving requests. IStorageService is
// scoped, so we open a temporary scope to resolve it at startup.
using (var scope = app.Services.CreateScope())
{
    var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
    await storage.EnsureBucketsExistAsync();
}

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
