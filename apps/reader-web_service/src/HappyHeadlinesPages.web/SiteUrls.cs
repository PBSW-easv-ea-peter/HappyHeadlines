using Microsoft.Extensions.Configuration;

namespace HappyHeadlinesPages.web;

// The journalist app (backstage) and the reader site are separate WASM apps on
// their own ports, so links between them have to be absolute - same hardcoded
// localhost convention as the service base URLs.
public static class SiteUrls
{
    private static IConfiguration? _configuration;

    public static void Initialize(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public static string Backstage => _configuration?["SiteUrls:Backstage"] ?? "http://localhost:8000";
    public static string ReaderSite => _configuration?["SiteUrls:ReaderSite"] ?? "http://localhost:8001";
}
