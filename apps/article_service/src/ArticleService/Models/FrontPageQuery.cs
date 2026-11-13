namespace ArticleService.Models;

// What a front page needs, chosen by the client from its own layout. The service knows nothing
// about the page - only "the newest Top, plus the newest PerSection in every section, plus the
// newest Top in Section if one is given" (GAF: Defer Fetching - fewer rows).
public sealed record FrontPageQuery(
    int Top,
    int PerSection,
    string? Section)
{
    public const int MaxTop = 50;
    public const int MaxPerSection = 20;

    public string? Validate()
    {
        if (Top is < 1 or > MaxTop) return $"top must be between 1 and {MaxTop}.";
        if (PerSection is < 0 or > MaxPerSection) return $"perSection must be between 0 and {MaxPerSection}.";
        // Part of the cache key, so keep it to plain section names.
        if (Section is not null && (Section.Length is 0 or > 40 || !Section.All(char.IsLetter)))
            return "section must be a section name (letters only).";
        return null;
    }

    public string CacheKeyPart =>
        $"{Top}:{PerSection}:{Section?.ToLowerInvariant() ?? "*"}";
}
