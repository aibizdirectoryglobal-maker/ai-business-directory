using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AiBusinessDirectory.Web.Data;
using AiBusinessDirectory.Web.Pages;
using Microsoft.EntityFrameworkCore;

var root = Directory.GetCurrentDirectory();
while (!File.Exists(Path.Combine(root, "dynamic", "AiBusinessDirectory.Web", "AiBusinessDirectory.Web.csproj")))
    root = Directory.GetParent(root)?.FullName ?? throw new Exception("Run from inside the repository.");
var appDll = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(root, "dynamic", "AiBusinessDirectory.Web", "bin", "Debug", "net10.0", "AiBusinessDirectory.Web.dll");
var host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
var evidence = Path.Combine(root, "dynamic", "test-results", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(evidence);
var database = Path.Combine(evidence, "requests.test.db");
DbContextOptions<ListingDbContext> Options(string path) => new DbContextOptionsBuilder<ListingDbContext>().UseSqlite($"Data Source={path}").Options;
await using (var seed = new ListingDbContext(Options(database)))
{
    await seed.Database.EnsureCreatedAsync();
    seed.ListingRequests.Add(new ListingRequest { BusinessName = "Pre-existing disposable sentinel" });
    await seed.SaveChangesAsync();
}
var assertions = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception("FAIL: " + message);
    assertions++;
    Console.WriteLine("PASS: " + message);
}
async Task<int> Count()
{
    await using var db = new ListingDbContext(Options(database));
    return await db.ListingRequests.CountAsync();
}
int FreePort()
{
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    var port = ((IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}
async Task<(Process process, HttpClient client)> Start(string dbPath, int limit = 1000, string? canonicalBase = null, bool productionProxy = false)
{
    var port = FreePort();
    var start = new ProcessStartInfo(host)
    {
        WorkingDirectory = Path.Combine(root, "dynamic", "AiBusinessDirectory.Web"),
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };
    start.ArgumentList.Add(appDll);
    start.Environment["ASPNETCORE_ENVIRONMENT"] = productionProxy ? "Production" : "Development";
    start.Environment["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = productionProxy ? "true" : "false";
    start.Environment["Storage__DatabasePath"] = dbPath;
    start.Environment["SubmissionLimits__PermitLimit"] = limit.ToString();
    start.Environment["SubmissionLimits__WindowSeconds"] = "60";
    start.Environment["PublicSite__AllowIndexing"] = "false";
    start.Environment["PORT"] = port.ToString();
    if (canonicalBase != null) start.Environment["PublicSite__BaseUrl"] = canonicalBase;
    var process = Process.Start(start)!;
    var log = new List<string>();
    process.OutputDataReceived += (_, e) => { if (e.Data != null) lock (log) log.Add(e.Data); };
    process.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (log) log.Add(e.Data); };
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() })
    {
        BaseAddress = new Uri($"http://127.0.0.1:{port}"),
        Timeout = TimeSpan.FromSeconds(15)
    };
    if (productionProxy) client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
    for (var attempt = 0; attempt < 150; attempt++)
    {
        try { if ((await client.GetAsync("/healthz")).IsSuccessStatusCode) return (process, client); }
        catch (HttpRequestException) { }
        if (process.HasExited) break;
        await Task.Delay(100);
    }
    if (!process.HasExited) process.Kill(true);
    throw new Exception("Application failed to start: " + string.Join(Environment.NewLine, log));
}
void Stop(Process process, HttpClient client)
{
    client.Dispose();
    if (!process.HasExited) { process.Kill(true); process.WaitForExit(); }
    process.Dispose();
}
var running = await Start(database);
try
{
    Check(await Count() == 1, "startup preserves pre-existing test data");
    foreach (var (route, source) in new[]
    {
        ("/", "index.html"), ("/index.html", "index.html"),
        ("/businesses/golden-harbor.html", "businesses/golden-harbor.html"),
        ("/comparisons/golden-harbor-vs-asm.html", "comparisons/golden-harbor-vs-asm.html"),
        ("/assets/styles.css", "assets/styles.css"),
        ("/google33bc54793ff2be2c.html", "google33bc54793ff2be2c.html")
    })
    {
        using var response = await running.client.GetAsync(route);
        Check(response.StatusCode == HttpStatusCode.OK, route + " returns 200");
        Check(await response.Content.ReadAsStringAsync() == await File.ReadAllTextAsync(Path.Combine(root, source)), route + " preserves source content/metadata");
        Check(response.Headers.GetValues("X-Robots-Tag").Single().Contains("noindex"), route + " is noindex on the test app");
    }
    Check((await running.client.GetAsync("/assets/listing.css")).IsSuccessStatusCode, "dynamic form CSS works");
    var sitemap = XDocument.Parse(await running.client.GetStringAsync("/sitemap.xml"));
    Check(sitemap.Descendants(XName.Get("loc", "http://www.sitemaps.org/schemas/sitemap/0.9")).Count() == 3, "sitemap preserves three canonical public URLs");
    Check((await running.client.GetStringAsync("/robots.txt")).Contains("Disallow: /"), "staging robots disallows crawling");
    foreach (var path in new[] { "/comparison-golden-harbor-asm.local.html", "/FORMSPREE-SETUP.md", "/appsettings.json", "/.git/config", "/data/requests.test.db", "/Program.cs" })
        Check((await running.client.GetAsync(path)).StatusCode == HttpStatusCode.NotFound, "private/development path excluded: " + path);

    var formHtml = await running.client.GetStringAsync("/request-listing.html");
    foreach (var route in new[] { "/", "/businesses/golden-harbor.html", "/comparisons/golden-harbor-vs-asm.html", "/request-listing.html" })
    {
        var html = await running.client.GetStringAsync(route);
        foreach (Match block in Regex.Matches(html, "<script[^>]*type=\"application/ld\\+json\"[^>]*>([\\s\\S]*?)</script>"))
        {
            using var json = JsonDocument.Parse(block.Groups[1].Value);
            Check(json.RootElement.ValueKind == JsonValueKind.Object, route + " JSON-LD parses");
        }
        var targets = Regex.Matches(html, "(?:href|src)=\"([^\"]+)\"").Select(m => WebUtility.HtmlDecode(m.Groups[1].Value)).Distinct();
        foreach (var target in targets)
        {
            if (Uri.TryCreate(target, UriKind.Absolute, out var absolute) && absolute.Scheme != "file") continue;
            var resolved = new Uri(new Uri(running.client.BaseAddress!, route), target);
            using var linked = await running.client.GetAsync(resolved.PathAndQuery);
            Check(linked.IsSuccessStatusCode, route + " local link works: " + target);
            if (resolved.Fragment.Length > 1)
            {
                var linkedHtml = await linked.Content.ReadAsStringAsync();
                Check(linkedHtml.Contains("id=\"" + resolved.Fragment[1..] + "\""), "fragment exists: " + target);
            }
        }
    }
    var token = WebUtility.HtmlDecode(Regex.Match(formHtml, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    Check(token.Length > 0, "Razor form generates antiforgery token");
    Check(!formHtml.Contains("Formspree", StringComparison.OrdinalIgnoreCase) && !formHtml.Contains("onsubmit=\"return false\""), "dynamic form uses only its own server-side handler");
    Dictionary<string, string> Fields() => new()
    {
        ["__RequestVerificationToken"] = token,
        ["Input.BusinessName"] = "Disposable Example Business",
        ["Input.OfficialWebsite"] = "https://example.org",
        ["Input.Country"] = "Malaysia",
        ["Input.BusinessCategory"] = "Software",
        ["Input.ContactEmail"] = "test@example.org",
        ["Input.Description"] = "Fictional data used only by local targeted tests.",
        ["Input.AdditionalNotes"] = "Disposable test record.",
        ["Input.Consent"] = "true"
    };
    async Task<HttpResponseMessage> Post(Dictionary<string, string> fields) =>
        await running.client.PostAsync("/request-listing.html", new FormUrlEncodedContent(fields));

    var missingToken = Fields(); missingToken.Remove("__RequestVerificationToken");
    Check((await Post(missingToken)).StatusCode == HttpStatusCode.BadRequest, "missing antiforgery token rejected");
    var badToken = Fields(); badToken["__RequestVerificationToken"] = "invalid";
    Check((await Post(badToken)).StatusCode == HttpStatusCode.BadRequest, "invalid antiforgery token rejected");
    foreach (var required in new[] { "BusinessName", "OfficialWebsite", "Country", "BusinessCategory", "ContactEmail", "Description" })
    {
        var fields = Fields(); fields["Input." + required] = "   ";
        Check((await Post(fields)).StatusCode == HttpStatusCode.BadRequest, "blank required field rejected: " + required);
    }
    foreach (var badUrl in new[] { "not-a-url", "ftp://example.org", "https://user:password@example.org" })
    {
        var fields = Fields(); fields["Input.OfficialWebsite"] = badUrl;
        Check((await Post(fields)).StatusCode == HttpStatusCode.BadRequest, "invalid/unsupported URL rejected");
    }
    var email = Fields(); email["Input.ContactEmail"] = "not-an-email";
    Check((await Post(email)).StatusCode == HttpStatusCode.BadRequest, "invalid email rejected");
    var consent = Fields(); consent["Input.Consent"] = "false";
    Check((await Post(consent)).StatusCode == HttpStatusCode.BadRequest, "missing consent rejected");
    var trap = Fields(); trap["Input.WebsiteFax"] = "bot content";
    Check((await Post(trap)).StatusCode == HttpStatusCode.BadRequest, "honeypot rejects spam");
    var longText = Fields(); longText["Input.Description"] = new string('x', 1001);
    Check((await Post(longText)).StatusCode == HttpStatusCode.BadRequest, "field length enforced server-side");
    var malicious = Fields(); malicious["Input.Description"] = "<script>alert('test')</script>"; malicious["Input.Country"] = "";
    var encoded = await (await Post(malicious)).Content.ReadAsStringAsync();
    Check(!encoded.Contains("<script>alert") && encoded.Contains("&lt;script&gt;"), "validation redisplay HTML-encodes submitted markup");
    using var oversized = await running.client.PostAsync("/request-listing.html", new StringContent(new string('x', 40000)));
    Check(oversized.StatusCode == HttpStatusCode.RequestEntityTooLarge, "32 KiB request limit enforced");
    Check(await Count() == 1, "all invalid submissions leave database unchanged");

    var valid = Fields(); valid["Input.Status"] = "Approved";
    using var saved = await Post(valid);
    Check(saved.StatusCode == HttpStatusCode.Redirect, "valid request redirects after saving");
    var success = await running.client.GetStringAsync(saved.Headers.Location!);
    Check(success.Contains("Test request saved") && success.Contains("Pending"), "successful save displays pending acknowledgement");
    await using (var inspect = new ListingDbContext(Options(database)))
    {
        var request = await inspect.ListingRequests.SingleAsync(r => r.BusinessName == "Disposable Example Business");
        Check(request.Status == "Pending", "client cannot approve or publish a request");
        Check(request.ContactEmail == valid["Input.ContactEmail"] && request.Description == valid["Input.Description"] && request.AdditionalNotes == valid["Input.AdditionalNotes"], "submitted fields saved accurately");
        Check(request.ConsentGiven && request.ConsentText == RequestListingModel.ConsentStatement && request.ConsentVersion == "listing-request-v1", "consent wording/version saved");
        Check(request.SubmittedAtUtc > DateTime.UtcNow.AddMinutes(-5) && request.ConsentAtUtc == request.SubmittedAtUtc, "UTC submission and consent timestamps saved");
    }
    await running.client.GetAsync("/request-listing.html");
    Check(await Count() == 2, "refresh does not resubmit or delete earlier data");
}
finally { Stop(running.process, running.client); }

running = await Start(database, canonicalBase: "https://directory.example/");
try
{
    Check(await Count() == 2, "restart preserves existing SQLite records at configured local path");
    var page = await running.client.GetStringAsync("/comparisons/golden-harbor-vs-asm.html");
    Check(page.Contains("href=\"https://directory.example/comparisons/golden-harbor-vs-asm.html\""), "explicit canonical base configuration is applied");
    Check((await running.client.GetStringAsync("/sitemap.xml")).Contains("https://directory.example/businesses/golden-harbor.html"), "sitemap uses configured canonical base, never request host");
}
finally { Stop(running.process, running.client); }

running = await Start(Path.Combine(evidence, "rate-limit.test.db"), limit: 2);
try
{
    for (var i = 0; i < 2; i++)
        Check((await running.client.PostAsync("/request-listing.html", new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode == HttpStatusCode.BadRequest, "initial request reaches antiforgery validation");
    using var limited = await running.client.PostAsync("/request-listing.html", new FormUrlEncodedContent(new Dictionary<string, string>()));
    Check(limited.StatusCode == HttpStatusCode.TooManyRequests, "submission rate limit returns 429");
    Check(limited.Headers.RetryAfter != null, "rate-limit response supplies Retry-After");
    Check((await running.client.GetAsync("/healthz")).IsSuccessStatusCode, "health checks are not blocked by submission limiter");
}
finally { Stop(running.process, running.client); }
running = await Start(Path.Combine(evidence, "proxy.test.db"), productionProxy: true);
try
{
    using var form = await running.client.GetAsync("/request-listing.html");
    Check(form.IsSuccessStatusCode, "production form works behind HTTPS-terminating proxy");
    var cookies = form.Headers.GetValues("Set-Cookie").ToArray();
    var antiforgeryCookie = cookies.Single(c => c.StartsWith("AIBD.Antiforgery="));
    Check(antiforgeryCookie.Contains("secure", StringComparison.OrdinalIgnoreCase) && antiforgeryCookie.Contains("httponly", StringComparison.OrdinalIgnoreCase), "production antiforgery cookie is Secure and HttpOnly");
    // Simulate the proxy's private HTTP hop after the browser sent its cookie over HTTPS.
    running.client.DefaultRequestHeaders.Add("Cookie", antiforgeryCookie.Split(';')[0]);
    var html = await form.Content.ReadAsStringAsync();
    var token = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    using var posted = await running.client.PostAsync("/request-listing.html", new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["__RequestVerificationToken"] = token, ["Input.BusinessName"] = "Proxy Test Only",
        ["Input.OfficialWebsite"] = "https://example.org", ["Input.Country"] = "Malaysia",
        ["Input.BusinessCategory"] = "Software", ["Input.ContactEmail"] = "test@example.org",
        ["Input.Description"] = "Disposable proxy test", ["Input.Consent"] = "true"
    }));
    Check(posted.StatusCode == HttpStatusCode.Redirect, "production antiforgery validation accepts valid HTTPS-proxied submission");
}
finally { Stop(running.process, running.client); }
Console.WriteLine($"ALL {assertions} TARGETED CHECKS PASSED. Disposable evidence retained at: {evidence}");
