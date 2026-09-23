using RabbitMQ.Client;

namespace Messaging.Exchanges;

public static class PublishedArticlesExchange
{
    public const string Name = "published_articles";

    public static async Task ConfigureAsync(IChannel channel)
    {
        await channel.ExchangeDeclareAsync(
            exchange: Name,
            type: ExchangeType.Topic,
            durable: false,
            autoDelete: false);
    }
}
