using Messaging.Events;
using Messaging.Exchanges;
using Messaging.RoutingKeys;
using Polly.CircuitBreaker;
using Polly.Timeout;
using PublishService.Shared;
using PublishService.Shared.Exceptions;
using PublishService.Shared.External;
using PublishService.Shared.Models;

namespace PublishService.Features.Publish;

public interface IPublishDraftHandler
{
    Task<IResult> PublishAsync(Guid id);
}

// Checks an approved draft for profanity, turns it into a PublishedArticleEvent on the queue,
// which ArticleService stores as an article, and then marks the draft as published in DraftService.
//
// The event is queued before the draft is marked, so a failure in between leaves the draft
// Approved and publishing it again is safe: ArticleService ignores a second event for the
// same DraftId. Marking first would risk a Published draft without an article.
public class PublishDraftHandler(
    IHttpDraftClient draftClient,
    ILogger<PublishDraftHandler> logger,
    IRabbitMqService rabbitMqService,
    IProfanityClient profanityClient)
    : IPublishDraftHandler
{
    public async Task<IResult> PublishAsync(Guid id)
    {
        DraftDTO? draft;
        try
        {
            draft = await draftClient.GetAsync(id);
        }
        catch (Exception ex) when (IsDraftServiceFailure(ex))
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

        // The gate before publishing (architecture handout). DraftService also checks on submit,
        // but only as early feedback for the editor (and that check fails open), while the banned-word
        // list can change after submit. Fails closed: if ProfanityService can't answer, nothing is
        // published, because an unchecked article on the site can't be taken back.
        var profanity = await profanityClient.CheckAsync($"{draft.Title}\n{draft.BreadText}");
        if (profanity.Unavailable)
        {
            throw new InfrastructureException("The article could not be checked for profanity. Please try again.");
        }

        if (profanity.IsProfane)
        {
            return await RefuseProfaneAsync(draft.Id, profanity.BannedWords);
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
        catch (Exception ex) when (IsDraftServiceFailure(ex))
        {
            logger.LogError(ex, "Draft {DraftId} was queued but could not be marked as published", id);
            throw new InfrastructureException(
                "The article was queued, but the draft could not be marked as published. Publish it again to retry.", ex);
        }

        return Results.Accepted(
            value: new PublishDraftResponse(draft.Id, publishedArticleEvent.Id, publishedArticleEvent.PublishDate));
    }

    // The draft is kept (still Approved) and the words are stored on it, so the editor can see
    // why it wasn't published. Storing them is best effort: the publish is refused either way.
    private async Task<IResult> RefuseProfaneAsync(Guid draftId, IReadOnlyList<string> bannedWords)
    {
        logger.LogInformation("Draft {DraftId} not published. {BannedWordCount} banned words found",
            draftId, bannedWords.Count);

        try
        {
            await draftClient.FlagProfanityAsync(draftId, bannedWords);
        }
        catch (Exception ex) when (IsDraftServiceFailure(ex))
        {
            logger.LogWarning("Could not store flagged words on draft {DraftId}. Reason: {Reason}",
                draftId, ex.GetType().Name);
        }

        return Results.Problem(
            detail: $"Not published: the article contains {bannedWords.Count} banned word(s): {string.Join(", ", bannedWords)}. The draft is kept with the words flagged.",
            statusCode: StatusCodes.Status422UnprocessableEntity);
    }

    // DraftService unreachable, error status after retries, circuit open, or an attempt timed out.
    private static bool IsDraftServiceFailure(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or BrokenCircuitException or TimeoutRejectedException;
}
