using System.Text;
using System.Text.Json;
using Messaging.Events;
using Messaging.Exchanges;
using Messaging.RoutingKeys;
using PublishService.Models;
using PublishService.Services.External;
using RabbitMQ.Client;

namespace PublishService.Services.Handlers;

public class PublishHandler(
    IDraftService draftService,
    ILogger<PublishHandler> logger,
    ConnectionFactory connectionFactory)
    : IPublishHandler
{
    public async Task<IResult> PublishAsync(Guid id)
    {
        DraftDTO? draft;
        try
        {
            draft = await draftService.GetDraftAsync(id);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError("A network error occurred while trying to get draft with ID {Id}. Exception: {ex}", id, ex);
            return Results.StatusCode(503);
        }
        catch (Exception ex)
        {
            logger.LogError("An unexpected error occurred while trying to get draft with ID {Id}. Exception: {ex}", id, ex);
            return Results.StatusCode(500);
        }

        if (draft == null)
        {
            logger.LogError("Failed to get draft with ID {Id}", id);
            return Results.NotFound("No draft found with that ID.");
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
        
        var json = JsonSerializer.Serialize(publishedArticleEvent); 
        var body = Encoding.UTF8.GetBytes(json);
        
        await using var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.BasicPublishAsync(
            exchange: PublishedArticlesExchange.Name,
            routingKey: PublishedArticleRKeys.ArticlePublished,
            body: body);
        
        return Results.Ok();
    }
}