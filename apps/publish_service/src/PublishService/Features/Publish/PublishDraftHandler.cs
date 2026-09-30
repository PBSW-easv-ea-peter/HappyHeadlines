using System.Text;
using System.Text.Json;
using Messaging.Events;
using Messaging.Exchanges;
using Messaging.RoutingKeys;
using PublishService.Shared;
using PublishService.Shared.Exceptions;
using PublishService.Shared.External;
using PublishService.Shared.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace PublishService.Features.Publish;

public interface IPublishDraftHandler
{
    Task<IResult> PublishAsync(Guid id);
}

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
        catch (HttpRequestException ex)
        {
            logger.LogError("A network error occurred while trying to get draft with ID {Id}. Exception: {ex}", id, ex);
            throw new InfrastructureException("A network error occurred while trying to get draft with ID {Id}.", ex);
        }

        if (draft == null)
        {
            return Results.NotFound();
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
        catch (BrokerUnreachableException e)
        {
            logger.LogError("Unreachable broker while trying to publish draft with ID {Id}. Exception: {ex}", id, e);
            throw new InfrastructureException("An error occurred while trying to publish draft with ID {Id}.", e);
        }
        
        return Results.Ok();
    }
}