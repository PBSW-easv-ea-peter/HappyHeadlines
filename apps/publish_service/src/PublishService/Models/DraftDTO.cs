namespace PublishService.Models;

public class DraftDTO
{
    public required Guid Id { get; init; }

    public required string JournalistName { get; init; }

    public required string SectionName { get; init; }

    public required string Title { get; init; }

    public required string Location { get; init; }

    public required DateTime CreatedDate { get; init; }

    public required string BreadText { get; init; }
}
