namespace DraftService.Models;

public enum DraftStatus
{
    WorkInProgress = 0,
    PendingApproval = 1,
    Approved = 2,
    Published = 3,
    Archived = 4
}

public class Draft
{
    public long Id { get; set; }
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
}
