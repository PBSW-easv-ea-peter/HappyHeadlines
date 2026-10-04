namespace DraftService.Models;

// Mirrors the sections seeded in ArticleService's database
// (database/migrations/article/V1__baseline_schema.sql). A draft only stores a section id,
// but a published article is matched to its section by name, so the two must line up.
public static class Sections
{
    private static readonly Dictionary<long, string> Names = new()
    {
        [1] = "Politics",
        [2] = "Technology",
        [3] = "Environment",
        [4] = "Health",
        [5] = "Culture"
    };

    public static bool IsValid(long id) => Names.ContainsKey(id);

    public static string? NameOf(long id) => Names.GetValueOrDefault(id);
}
