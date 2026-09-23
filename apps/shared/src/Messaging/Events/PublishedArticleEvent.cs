namespace Messaging.Events;

public class PublishedArticleEvent
{
    public required Guid id { get; init; }

    public required Guid DraftId { get; init; }

    public required string JournalistName { get; init; }

    public required string SectionName { get; init; }

    public required string Title { get; init; }

    public required string Location { get; init; }

    public required DateTime CreatedDate { get; init; }

    public required DateTime PublishDate { get; init; }

    public required string BreadText { get; init; }
}
