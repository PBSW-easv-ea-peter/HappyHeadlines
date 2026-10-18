using HappyHeadlinesPages.web.Layout;

namespace HappyHeadlinesPages.web.Models;

// Mirrors database/queries/article/seed_reference_data.sql - a Draft only
// carries a SectionId, this resolves it to a display name for the dashboard
// and provides the option list for the create/edit draft forms. The color is
// a small editorial convention (category color-coding) reused on article cards.
public static class Sections
{
    public static readonly IReadOnlyList<(long Id, string Name, string Color)> All =
    [
        (1, "Politics", CustomMudTheme.LogoBlue),
        (2, "Technology", CustomMudTheme.LogoPurple),
        (3, "Environment", CustomMudTheme.LogoGreen),
        (4, "Health", CustomMudTheme.LogoPink),
        (5, "Culture", CustomMudTheme.LogoOrange),
    ];

    public static string NameOf(long id) =>
        All.Where(s => s.Id == id).Select(s => s.Name).FirstOrDefault() ?? $"Section #{id}";

    public static string ColorOf(long id) =>
        All.Where(s => s.Id == id).Select(s => s.Color).FirstOrDefault() ?? "#9E9E9E";

    // Article DTOs only carry a section name, not an id.
    public static string ColorOfName(string name) =>
        All.Where(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
           .Select(s => s.Color)
           .FirstOrDefault() ?? "#9E9E9E";
}
