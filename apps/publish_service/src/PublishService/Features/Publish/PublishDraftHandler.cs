using Messaging.Events;
using Messaging.Exchanges;
using Messaging.RoutingKeys;
using PublishService.Shared;
using PublishService.Shared.Exceptions;
using PublishService.Shared.External;
using PublishService.Shared.Models;

namespace PublishService.Features.Publish;

public interface IPublishDraftHandler
{
    Task<IResult> PublishAsync(Guid id);
}

// Turns an approved draft into a PublishedArticleEvent on the queue, which ArticleService
// stores as an article, and then marks the draft as published in DraftService.
//
// The event is queued before the draft is marked, so a failure in between leaves the draft
// Approved and publishing it again is safe: ArticleService ignores a second event for the
// same DraftId. Marking first would risk a Published draft without an article.
public class PublishDraftHandler(
    IHttpDraftClient draftClient,
    ILogger<PublishDraftHandler> logger,
    IRabbitMqService rabbitMqService)
    : IPublishDraftHandler
{
    public async Task<IResult> PublishAsync(Guid id)
    {
        DraftDTO? draft;
        try
        {
            draft = await draftClient.GetAsync(id);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Could not get draft {DraftId} from DraftService", id);
            throw new InfrastructureException("DraftService is unavailable. Please try again.", ex);
        }

        if (draft == null)
        {
            return Results.NotFound();
        }

        if (draft.Status != DraftStatus.Approved)
        {
            return Results.Conflict(draft.Status == DraftStatus.Published
                ? "The draft is already published."
                : $"Only approved drafts can be published. This draft is {draft.Status}.");
        }

        PublishedArticleEvent publishedArticleEvent = new()
        {
            Id = Guid.NewGuid(),
            DraftId = draft.Id,
            JournalistName = draft.JournalistName,
            SectionName = draft.SectionName,
            Title = draft.Title,
            Location = draft.Location,
            CreatedDate = draft.CreatedDate,
            PublishDate = DateTime.UtcNow,
            BreadText = draft.BreadText
        };

        try
        {
            await rabbitMqService.PublishAsync(
                message: publishedArticleEvent,
                exchange: PublishedArticlesExchange.Name,
                routingKey: PublishedArticleRKeys.ArticlePublished);
        }
        catch (Exception ex)
        {
            // Broker down, message not confirmed, or no queue bound to receive it.
            logger.LogError(ex, "Could not queue draft {DraftId} for publishing", id);
            throw new InfrastructureException("The article could not be queued for publishing. Please try again.", ex);
        }

        logger.LogInformation("Draft {DraftId} queued for publishing as event {EventId}",
            draft.Id, publishedArticleEvent.Id);

        try
        {
            await draftClient.MarkPublishedAsync(id);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Draft {DraftId} was queued but could not be marked as published", id);
            throw new InfrastructureException(
                "The article was queued, but the draft could not be marked as published. Publish it again to retry.", ex);
        }

        return Results.Accepted(
            value: new PublishDraftResponse(draft.Id, publishedArticleEvent.Id, publishedArticleEvent.PublishDate));
    }
}
