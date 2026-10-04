namespace PublishService.Shared.Models;

// Mirrors DraftService's DraftStatus. Only Approved drafts can be published.
public enum DraftStatus
{
    WorkInProgress = 0,
    PendingApproval = 1,
    Approved = 2,
    Published = 3,
    Archived = 4
}

// DraftService's FilledInDraft (GET /api/drafts/filled-in-draft/{id}).
public class DraftDTO
{
    public required Guid Id { get; init; }

    public required string JournalistName { get; init; }

    public required string SectionName { get; init; }

    public required string Title { get; init; }

    public required string Location { get; init; }

    public required DateTime CreatedDate { get; init; }

    public required string BreadText { get; init; }

    public required DraftStatus Status { get; init; }
}
