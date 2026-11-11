using System.Text.Json;
using Messaging.Events;
using Messaging.Exchanges;
using Messaging.RoutingKeys;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NewsletterService.Queues.NewSubscriber;

/// <summary>
/// Background service that consumes new subscriber messages from RabbitMQ.
/// This class handles the RabbitMQ infrastructure and delegates message processing to a handler.
/// </summary>
public class NewSubscriberConsumer : BackgroundService
{
    // Owned by ArticleService, not the Messaging lib - the queue is this consumer's concern.
    private const string QueueName = "newsletter_service.new_subscribers";

    private readonly ConnectionFactory _connectionFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NewSubscriberConsumer> _logger;

    public NewSubscriberConsumer(
        ConnectionFactory connectionFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<NewSubscriberConsumer> logger
        )
    {
        _connectionFactory = connectionFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using IConnection connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await SubscribersExchange.ConfigureAsync(channel);
        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);
        
        await channel.QueueBindAsync(
            queue: QueueName,
            exchange: SubscribersExchange.Name,
            routingKey: SubscriberRKeys.NewSubscriber,
            cancellationToken: stoppingToken);

        // At most 10 unacked messages per consumer, so work is spread across replicas.
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, stoppingToken);

        AsyncEventingBasicConsumer consumer = new(channel);
        consumer.ReceivedAsync += async (_, delivery) => await HandleMessageAsync(channel, delivery, stoppingToken);

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, stoppingToken);
        _logger.LogInformation("Consuming {Queue}", QueueName);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutting down
        }
    }
    
    public async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken ct)
    {
        try
        {
            NewSubscriberEvent newSubscriber = JsonSerializer.Deserialize<NewSubscriberEvent>(delivery.Body.Span)
                ?? throw new JsonException("Message body was null.");

            _logger.LogInformation("Subscriber email: {Email}", newSubscriber.Email);

            // Delegate message handling to the message handler
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            IMessageHandler<NewSubscriberEvent> messageHandler = scope.ServiceProvider.GetRequiredService<IMessageHandler<NewSubscriberEvent>>();
            await messageHandler.HandleAsync(newSubscriber, ct);

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, ct);
        }
        catch (JsonException ex)
        {
            // Poison message (bad JSON): retrying will never succeed.
            _logger.LogWarning(ex, "Discarding unprocessable message {DeliveryTag}", delivery.DeliveryTag);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, ct);
        }
        catch (Exception ex)
        {
            // Likely transient - put it back so it can be retried.
            _logger.LogError(ex, "Failed to process message {DeliveryTag} - requeueing", delivery.DeliveryTag);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, ct);
        }
    }
}
