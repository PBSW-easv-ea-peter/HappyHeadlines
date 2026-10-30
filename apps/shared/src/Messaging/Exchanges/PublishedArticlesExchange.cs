using RabbitMQ.Client;

namespace Messaging.Exchanges;

public static class PublishedArticlesExchange
{
    public const string Name = "published_articles";

    // Durable like the queues bound to it and the persistent messages sent through it - otherwise
    // the exchange disappears on a broker restart. Changing an existing exchange's durability
    // fails with PRECONDITION_FAILED, so an old broker needs the exchange deleted once.
    public static async Task ConfigureAsync(IChannel channel)
    {
        await channel.ExchangeDeclareAsync(
            exchange: Name,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);
    }
}
