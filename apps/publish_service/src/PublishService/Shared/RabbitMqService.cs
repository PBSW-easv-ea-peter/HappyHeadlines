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
    public async Task PublishAsync(
        object message,
        string exchange,
        string routingKey)
    {
        var json = JsonSerializer.Serialize(message); 
        var body = Encoding.UTF8.GetBytes(json);
        
        await using var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            body: body);
    }
}