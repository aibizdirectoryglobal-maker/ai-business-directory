namespace AiBusinessDirectory.Web;

// The published artifacts are copies of an explicit source allowlist. Source HTML is never edited.
public sealed class PublicContent
{
    public const string OriginalBaseUrl = "https://aibizdirectoryglobal-maker.github.io/ai-business-directory/";
    private readonly string root = Path.Combine(AppContext.BaseDirectory, "Public");
    public string BaseUrl { get; }
    public bool AllowIndexing { get; }

    public PublicContent(IConfiguration configuration)
    {
        var configured = configuration["PublicSite:BaseUrl"] ?? OriginalBaseUrl;
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.UserInfo.Length > 0 ||
            configured.Any(char.IsWhiteSpace))
            throw new InvalidOperationException("PublicSite:BaseUrl must be an absolute HTTPS canonical base URL without credentials, query or fragment.");

        BaseUrl = uri.AbsoluteUri.TrimEnd('/') + "/";
        AllowIndexing = configuration.GetValue<bool>("PublicSite:AllowIndexing");
        if (AllowIndexing && (uri.Host.EndsWith(".onrender.com", StringComparison.OrdinalIgnoreCase) || uri.IsLoopback))
            throw new InvalidOperationException("Temporary hosting URLs must not be enabled for indexing.");
    }

    public string AssetsDirectory => Path.Combine(root, "assets");

    public async Task<string> ReadAsync(string relativePath, bool rewriteBaseUrl = true)
    {
        var text = await File.ReadAllTextAsync(Path.Combine(root, relativePath));
        return rewriteBaseUrl ? text.Replace(OriginalBaseUrl, BaseUrl, StringComparison.Ordinal) : text;
    }
}
