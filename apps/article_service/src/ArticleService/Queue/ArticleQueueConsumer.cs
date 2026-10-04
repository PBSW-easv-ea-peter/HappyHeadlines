using System.Text.Json;
using ArticleService.Repositories;
using ArticleService.Setup;
using Messaging.Events;
using Messaging.Exchanges;
using Messaging.RoutingKeys;
using Polly.CircuitBreaker;
using Polly.Registry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ArticleService.Queue;

// Subscribes to published_articles/article.published and stores each article in its shard.
// All replicas consume from the same named queue (competing consumers), so each article
// is stored once. Messages are acked only after the write, so redeliveries are expected -
// CreatePublishedAsync is idempotent on DraftId.
//
// Fault isolation: writes go through a circuit breaker per shard (ShardResilience). While a
// shard's circuit is open its messages are requeued immediately and bounce between broker
// and consumer until the circuit half-opens. Fine at ArticleService's low publish rate; if
// volume grows, replace the requeue with a delayed retry queue (dead-letter exchange + TTL).
// See docs/plan-articleservice-subscriber.md.
public class ArticleQueueConsumer : BackgroundService
{
    // Owned by ArticleService, not the Messaging lib - the queue is this consumer's concern.
    public const string QueueName = "article_service.published_articles";

    private static readonly TimeSpan CircuitOpenRequeueDelay = TimeSpan.FromSeconds(5);

    private readonly ConnectionFactory _connectionFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ResiliencePipelineProvider<string> _pipelines;
    private readonly ILogger<ArticleQueueConsumer> _logger;

    public ArticleQueueConsumer(
        ConnectionFactory connectionFactory,
        IServiceScopeFactory scopeFactory,
        ResiliencePipelineProvider<string> pipelines,
        ILogger<ArticleQueueConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _scopeFactory = scopeFactory;
        _pipelines = pipelines;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Long-lived connection/channel: the broker pushes messages to us for as long as we run.
        await using var connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await PublishedArticlesExchange.ConfigureAsync(channel);
        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);
        await channel.QueueBindAsync(
            queue: QueueName,
            exchange: PublishedArticlesExchange.Name,
            routingKey: PublishedArticleRKeys.ArticlePublished,
            cancellationToken: stoppingToken);

        // At most 10 unacked messages per consumer, so work is spread across replicas.
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => HandleAsync(channel, delivery);

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

    // Internal so ArticleService.Tests can verify the ack/nack decisions without a broker.
    internal async Task HandleAsync(IChannel channel, BasicDeliverEventArgs delivery)
    {
        try
        {
            var published = JsonSerializer.Deserialize<PublishedArticleEvent>(delivery.Body.Span)
                ?? throw new JsonException("Message body was null.");

            // Repositories are scoped; a BackgroundService is a singleton, so create a scope per message.
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IArticleWriteRepository>();

            // An unknown location has no pipeline - that is a poison message, not a transient failure.
            if (!_pipelines.TryGetPipeline(ShardResilience.PipelineKey(published.Location), out var pipeline))
            {
                throw new ArgumentException($"Unknown location '{published.Location}'.");
            }

            var created = await pipeline.ExecuteAsync(
                async _ => await repository.CreatePublishedAsync(published));
            if (created)
            {
                _logger.LogInformation("Stored article for draft {DraftId} in {Location}",
                    published.DraftId, published.Location);
            }
            else
            {
                _logger.LogInformation("Article for draft {DraftId} already stored - ignoring redelivery",
                    published.DraftId);
            }

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false);
        }
        catch (BrokenCircuitException)
        {
            // Shard is known to be down - requeue without touching it. Debug only: the circuit
            // opening is already logged once by ShardResilience.
            _logger.LogDebug("Circuit open - requeueing message {DeliveryTag}", delivery.DeliveryTag);

            // Without a pause the requeued message is redelivered immediately (~250/s in test 6).
            // Trade-off: the consumer handles one message at a time, so this also delays messages
            // for the other shards. A TTL retry queue would avoid that (see the test plan).
            await Task.Delay(CircuitOpenRequeueDelay);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            // Poison message (bad JSON, unknown location or section): retrying will never succeed.
            _logger.LogWarning(ex, "Discarding unprocessable message {DeliveryTag}", delivery.DeliveryTag);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            // Likely transient (e.g. shard unavailable) - put it back so it can be retried.
            _logger.LogError(ex, "Failed to store message {DeliveryTag} - requeueing", delivery.DeliveryTag);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true);
        }
    }
}
