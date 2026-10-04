using HappyHeadlinesPages.web.Models;

namespace HappyHeadlines.reader.wasm;

// The reader picks an "edition" - one of the ArticleService shards. Global is
// the default and is listed first; anything unknown in the query string falls
// back to it rather than hitting ArticleService with an invalid location.
public static class ReaderRegions
{
    public const string Default = "GO";

    public static readonly IReadOnlyList<(string Code, string Name)> Ordered =
        Locations.All.OrderBy(l => l.Code != Default).ToList();

    public static string Normalize(string? code) =>
        Locations.All.Any(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase))
            ? code!.ToUpperInvariant()
            : Default;

    public static string NameOf(string code) =>
        Locations.All.Where(l => l.Code == code).Select(l => l.Name).FirstOrDefault() ?? code;
}
