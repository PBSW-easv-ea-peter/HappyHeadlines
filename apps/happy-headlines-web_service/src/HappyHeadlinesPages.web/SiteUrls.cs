namespace HappyHeadlinesPages.web;

// The journalist app (backstage) and the reader site are separate WASM apps on
// their own ports, so links between them have to be absolute - same hardcoded
// localhost convention as the service base URLs.
public static class SiteUrls
{
    public const string Backstage = "http://localhost:8000";
    public const string ReaderSite = "http://localhost:8001";
}
