namespace PublishService.Features.Publish;

// The article is created asynchronously by ArticleService once it consumes the event,
// so the response only confirms that the event is on the queue.
public record PublishDraftResponse(Guid DraftId, Guid EventId, DateTime PublishDate);
