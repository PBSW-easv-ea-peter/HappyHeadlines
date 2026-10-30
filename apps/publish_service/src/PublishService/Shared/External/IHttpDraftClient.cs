using PublishService.Shared.Models;

namespace PublishService.Shared.External;

public interface IHttpDraftClient
{
    // Null when the draft doesn't exist.
    Task<DraftDTO?> GetAsync(Guid id);

    // Moves the draft from Approved to Published. False when DraftService refuses the
    // transition (409), e.g. because the draft was published or archived concurrently.
    Task<bool> MarkPublishedAsync(Guid id);

    // Stores the banned words found before publishing on the draft, which stays Approved.
    // False when DraftService refuses (409), e.g. because the draft is no longer Approved.
    Task<bool> FlagProfanityAsync(Guid id, IReadOnlyList<string> flaggedWords);
}
