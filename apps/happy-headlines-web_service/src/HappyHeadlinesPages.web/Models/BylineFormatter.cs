namespace HappyHeadlinesPages.web.Models;

// Mirrors DraftService's BylineGenerator so the UI can preview a regenerated
// byline without a round trip - the server remains the source of truth on save.
public static class BylineFormatter
{
    public static string Generate(IEnumerable<Journalist> journalists)
    {
        var names = journalists
            .OrderBy(LastName)
            .Select(j => j.Name)
            .ToList();

        return names.Count switch
        {
            0 => string.Empty,
            1 => names[0],
            _ => string.Join(", ", names.Take(names.Count - 1)) + " & " + names[^1]
        };
    }

    private static string LastName(Journalist journalist)
    {
        var parts = journalist.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : journalist.Name;
    }
}
