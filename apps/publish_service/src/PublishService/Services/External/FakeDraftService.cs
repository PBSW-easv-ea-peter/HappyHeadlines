using PublishService.Models;

namespace PublishService.Services.External;

public class FakeDraftService : IDraftService
{
    private static readonly List<Draft> Drafts =
    [
        new Draft
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            JournalistName = "Sophie Martin",
            SectionName = "Technology",
            Title = "AI Is Changing How We Build Software",
            Location = "Paris",
            CreatedDate = new DateTime(2026, 9, 20, 10, 30, 0),
            BreadText = "Artificial intelligence is becoming an increasingly important part of modern software development."
        },
        new Draft
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            JournalistName = "Thomas Bernard",
            SectionName = "Science",
            Title = "New Telescope Reveals Distant Galaxies",
            Location = "Lyon",
            CreatedDate = new DateTime(2026, 9, 21, 14, 15, 0),
            BreadText = "Astronomers have captured new observations that provide further insight into the early universe."
        }
    ];

    public Task<Draft?> GetDraftAsync(Guid id)
    {
        var draft = Drafts.FirstOrDefault(d => d.Id == id);

        return Task.FromResult(draft);
    }
}