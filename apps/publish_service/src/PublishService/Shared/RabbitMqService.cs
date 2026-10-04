using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace PublishService.Shared;

public interface IRabbitMqService
{
    Task PublishAsync(
        object message,
        string exchange,
        string routingKey);
}

public class RabbitMqService(
    ConnectionFactory connectionFactory)
    : IRabbitMqService
{
    // Publisher confirms: BasicPublishAsync waits until the broker has taken responsibility
    // for the message, and throws if it was nacked or - because it's mandatory - couldn't be
    // routed to any queue (e.g. ArticleService has never declared its queue). Without them a
    // lost message would look like a successful publish.
    private static readonly CreateChannelOptions ConfirmedChannel = new(
        publisherConfirmationsEnabled: true,
        publisherConfirmationTrackingEnabled: true);

    public async Task PublishAsync(
        object message,
        string exchange,
        string routingKey)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        // Persistent, so a message waiting in a durable queue survives a broker restart.
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            Persistent = true
        };

        await using var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync(ConfirmedChannel);
        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: true,
            basicProperties: properties,
            body: body);
    }
}
