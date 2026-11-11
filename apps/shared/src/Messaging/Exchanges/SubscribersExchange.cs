using RabbitMQ.Client;

namespace Messaging.Exchanges;

public static class SubscribersExchange
{
    public const string Name = "subscribers";

    public static async Task ConfigureAsync(IChannel channel)
    {
        await channel.ExchangeDeclareAsync(
            exchange: Name,
            type: ExchangeType.Topic,
            durable: false,
            autoDelete: false);
    }
}
