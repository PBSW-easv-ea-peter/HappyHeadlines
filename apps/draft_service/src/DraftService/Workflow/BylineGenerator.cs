using DraftService.Models;

namespace DraftService.Workflow;

// Turns the credited-authors list into a display byline, sorted by last name.
// Kept separate from the workflow tracking fields (CreatedBy/LastEditedBy/ApprovedBy) -
// who gets credited is a deliberate choice, not necessarily whoever last touched the draft.
public static class BylineGenerator
{
    public static string Generate(IEnumerable<Journalist> journalists)
    {
        var names = journalists
            .OrderBy(LastName, StringComparer.OrdinalIgnoreCase)
            .Select(j => j.Name)
            .ToList();

        return names.Count switch
        {
            0 => string.Empty,
            1 => names[0],
            _ => string.Join(", ", names.Take(names.Count - 1)) + " & " + names[^1]
        };
    }

    public static string LastName(Journalist journalist)
    {
        var lastSpace = journalist.Name.LastIndexOf(' ');
        return lastSpace >= 0 ? journalist.Name[(lastSpace + 1)..] : journalist.Name;
    }
}
