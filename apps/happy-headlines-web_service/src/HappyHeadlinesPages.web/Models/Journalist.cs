namespace HappyHeadlinesPages.web.Models;

public record Journalist(long Id, string Name);

// Placeholder for a real login system: mirrors the journalists seeded by
// database/queries/article/seed_reference_data.sql so demo data lines up
// across the Articles and Drafts pages.
public static class Journalists
{
    public static readonly IReadOnlyList<Journalist> All =
    [
        new(1, "Emma Johnson"),
        new(2, "Liam Chen"),
        new(3, "Sophia Garcia"),
        new(4, "Noah Wilson"),
        new(5, "Olivia Brown"),
        new(6, "James Lee"),
        new(7, "Ava Martinez"),
        new(8, "Ethan Davis"),
    ];

    public static string NameOf(long id) =>
        All.FirstOrDefault(j => j.Id == id)?.Name ?? $"Journalist #{id}";
}
