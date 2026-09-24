namespace DraftService.Models;

public enum DraftStatus
{
    WorkInProgress = 0,
    PendingApproval = 1,
    Approved = 2,
    Published = 3,
    Archived = 4
}

public class FilledInDraft
{
    public required Guid Id { get; init; }

    public required string JournalistName { get; init; }

    public required string SectionName { get; init; }

    public required string Title { get; init; }

    public required string Location { get; init; }

    public required DateTime CreatedDate { get; init; }

    public required string BreadText { get; init; }
}

public class Draft
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Breadtext { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public long SectionId { get; set; }
    public long CreatedByJournalistId { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public long LastEditedByJournalistId { get; set; }
    public DateTimeOffset LastEditedDate { get; set; }
    public long? ApprovedByJournalistId { get; set; }
    public DateTimeOffset? ApprovedDate { get; set; }
    public string[] FlaggedWords { get; set; } = [];
    public DraftStatus Status { get; set; }
    public string? ReviewNote { get; set; }
}

public class CreateDraftRequest
{
    public long JournalistId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Breadtext { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public long SectionId { get; set; }
}

public class EditDraftRequest
{
    public long JournalistId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Breadtext { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public long SectionId { get; set; }
}

public class ApproveDraftRequest
{
    public long JournalistId { get; set; }
    public string? Note { get; set; }
}

public class RejectDraftRequest
{
    public string? Note { get; set; }
}
