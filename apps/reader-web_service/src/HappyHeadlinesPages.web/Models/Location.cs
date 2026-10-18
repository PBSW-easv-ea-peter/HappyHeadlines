namespace HappyHeadlinesPages.web.Models;

// The region codes ArticleService/ArticleSeeder use (EU, NA, SA, ...) - the
// create/edit draft forms reuse them so a published draft's Location lines
// up with a real shard if the DraftService/ArticleService gap ever closes.
public static class Locations
{
    public static readonly IReadOnlyList<(string Code, string Name)> All =
    [
        ("EU", "Europe"),
        ("NA", "North America"),
        ("SA", "South America"),
        ("AU", "Australia"),
        ("AS", "Asia"),
        ("AN", "Antarctica"),
        ("AF", "Africa"),
        ("GO", "Global"),
    ];
}
