using System.Globalization;
using System.Threading.RateLimiting;
using AiBusinessDirectory.Web;
using AiBusinessDirectory.Web.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } assignedPort)
{
    if (!int.TryParse(assignedPort, out var port) || port is < 1 or > 65535)
        throw new InvalidOperationException("PORT must be an integer from 1 to 65535.");
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

const long maximumBodyBytes = 32 * 1024;
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = maximumBodyBytes);
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueCountLimit = 32;
    options.KeyLengthLimit = 128;
    options.ValueLengthLimit = 4096;
    options.MultipartBodyLengthLimit = maximumBodyBytes;
});
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "AIBD.Antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});

var databasePath = Path.GetFullPath(builder.Configuration["Storage:DatabasePath"] ?? "App_Data/listing-requests.test.db",
    builder.Environment.ContentRootPath);
var storageDirectory = Path.GetDirectoryName(databasePath)!;
Directory.CreateDirectory(storageDirectory);
var keyDirectory = Directory.CreateDirectory(Path.Combine(storageDirectory, "keys"));
builder.Services.AddDataProtection().SetApplicationName("AiBusinessDirectory.Phase1")
    .PersistKeysToFileSystem(keyDirectory);
var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
builder.Services.AddDbContext<ListingDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddSingleton<PublicContent>();

var permitLimit = builder.Configuration.GetValue("SubmissionLimits:PermitLimit", 20);
var windowSeconds = builder.Configuration.GetValue("SubmissionLimits:WindowSeconds", 60);
if (permitLimit is < 1 or > 1000 || windowSeconds is < 1 or > 3600)
    throw new InvalidOperationException("Submission limits are outside the supported range.");
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        HttpMethods.IsPost(context.Request.Method) && context.Request.Path == "/request-listing.html"
            ? RateLimitPartition.GetFixedWindowLimiter("listing-submissions", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            })
            : RateLimitPartition.GetNoLimiter("other-requests"));
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        await context.HttpContext.Response.WriteAsync("Too many listing requests. Please try again later.", cancellationToken);
    };
});

var app = builder.Build();
var publicContent = app.Services.GetRequiredService<PublicContent>();
using (var scope = app.Services.CreateScope())
{
    // Phase 1: initialize an absent database only. Never delete, reset or reseed an existing database.
    var db = scope.ServiceProvider.GetRequiredService<ListingDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Use(async (context, next) =>
{
    if (!publicContent.AllowIndexing)
        context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    if (context.Request.Path == "/request-listing.html")
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.ContentSecurityPolicy = "form-action 'self'; frame-ancestors 'none'";
    }
    if (context.Request.ContentLength > maximumBodyBytes)
    {
        context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
        await context.Response.WriteAsync("Request is too large.");
        return;
    }
    await next();
});
app.UseRateLimiter();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(publicContent.AssetsDirectory),
    RequestPath = "/assets"
});
foreach (var (route, file) in new[]
{
    ("/", "index.html"),
    ("/index.html", "index.html"),
    ("/businesses/golden-harbor.html", "businesses/golden-harbor.html"),
    ("/comparisons/golden-harbor-vs-asm.html", "comparisons/golden-harbor-vs-asm.html")
})
    app.MapGet(route, async () => Results.Content(await publicContent.ReadAsync(file), "text/html; charset=utf-8"));

app.MapGet("/google33bc54793ff2be2c.html", async () =>
    Results.Content(await publicContent.ReadAsync("google33bc54793ff2be2c.html", false), "text/html; charset=utf-8"));
app.MapGet("/sitemap.xml", async () => Results.Content(await publicContent.ReadAsync("sitemap.xml"), "application/xml; charset=utf-8"));
app.MapGet("/robots.txt", () => Results.Text(publicContent.AllowIndexing
    ? $"User-agent: *\nAllow: /\n\nSitemap: {publicContent.BaseUrl}sitemap.xml\n"
    : "User-agent: *\nDisallow: /\n", "text/plain"));
app.MapGet("/healthz", async (ListingDbContext db) => await db.Database.CanConnectAsync()
    ? Results.Ok(new { status = "ok", phase = "disposable-test" })
    : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));
app.MapRazorPages();
app.Run();
