using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ArticleService.Queue;
using ArticleService.Repositories;
using ArticleService.Setup;
using Messaging.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace ArticleService.Tests.Queue;

// Verifies the ack/nack decision for each outcome of handling a message.
// Ack = done (delete); Nack(requeue: true) = retry later; Nack(requeue: false) = discard (poison).
public class ArticleQueueConsumerTests
{
    private const ulong DeliveryTag = 42;

    private readonly Mock<IArticleWriteRepository> _repository = new();
    private readonly Mock<IChannel> _channel = new();
    private readonly ArticleQueueConsumer _consumer;

    public ArticleQueueConsumerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_repository.Object);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        // Pass-through pipeline for EU only, so an unknown location has no pipeline -
        // same as in production, where there is one pipeline per configured shard.
        var pipelines = new ResiliencePipelineRegistry<string>();
        pipelines.TryAddBuilder(ShardResilience.PipelineKey("EU"), (_, _) => { });

        _consumer = new ArticleQueueConsumer(
            new ConnectionFactory(), scopeFactory, pipelines, NullLogger<ArticleQueueConsumer>.Instance);
    }

    [Fact]
    public async Task HandleAsync_NewArticle_Acks()
    {
        _repository.Setup(r => r.CreatePublishedAsync(It.IsAny<PublishedArticleEvent>())).ReturnsAsync(true);

        await _consumer.HandleAsync(_channel.Object, Delivery(Event()));

        VerifyAcked();
    }

    [Fact]
    public async Task HandleAsync_AlreadyStored_AcksWithoutError()
    {
        // Redelivered message: the article exists, so there is nothing left to do.
        _repository.Setup(r => r.CreatePublishedAsync(It.IsAny<PublishedArticleEvent>())).ReturnsAsync(false);

        await _consumer.HandleAsync(_channel.Object, Delivery(Event()));

        VerifyAcked();
    }

    [Fact]
    public async Task HandleAsync_InvalidJson_NacksWithoutRequeue()
    {
        await _consumer.HandleAsync(_channel.Object, Delivery("not json"));

        VerifyNacked(requeue: false);
        _repository.Verify(r => r.CreatePublishedAsync(It.IsAny<PublishedArticleEvent>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UnknownLocation_NacksWithoutRequeue()
    {
        await _consumer.HandleAsync(_channel.Object, Delivery(Event(location: "XX")));

        VerifyNacked(requeue: false);
        _repository.Verify(r => r.CreatePublishedAsync(It.IsAny<PublishedArticleEvent>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UnknownSection_NacksWithoutRequeue()
    {
        _repository.Setup(r => r.CreatePublishedAsync(It.IsAny<PublishedArticleEvent>()))
            .ThrowsAsync(new ArgumentException("Unknown section 'Sport'."));

        await _consumer.HandleAsync(_channel.Object, Delivery(Event()));

        VerifyNacked(requeue: false);
    }

    [Fact]
    public async Task HandleAsync_CircuitOpen_NacksWithRequeue()
    {
        _repository.Setup(r => r.CreatePublishedAsync(It.IsAny<PublishedArticleEvent>()))
            .ThrowsAsync(new BrokenCircuitException());

        await _consumer.HandleAsync(_channel.Object, Delivery(Event()));

        VerifyNacked(requeue: true);
    }

    [Fact]
    public async Task HandleAsync_ShardUnreachable_NacksWithRequeue()
    {
        _repository.Setup(r => r.CreatePublishedAsync(It.IsAny<PublishedArticleEvent>()))
            .ThrowsAsync(new NpgsqlException("Connection refused", new SocketException()));

        await _consumer.HandleAsync(_channel.Object, Delivery(Event()));

        VerifyNacked(requeue: true);
    }

    private static PublishedArticleEvent Event(string location = "EU") => new()
    {
        Id = Guid.NewGuid(),
        DraftId = Guid.NewGuid(),
        JournalistName = "Test Journalist",
        SectionName = "Politics",
        Title = "Test title",
        Location = location,
        CreatedDate = DateTime.UtcNow,
        PublishDate = DateTime.UtcNow,
        BreadText = "Test body"
    };

    private static BasicDeliverEventArgs Delivery(PublishedArticleEvent published) =>
        Delivery(JsonSerializer.Serialize(published));

    private static BasicDeliverEventArgs Delivery(string body) => new(
        consumerTag: "test",
        deliveryTag: DeliveryTag,
        redelivered: false,
        exchange: "published_articles",
        routingKey: "article.published",
        properties: new BasicProperties(),
        body: Encoding.UTF8.GetBytes(body));

    private void VerifyAcked()
    {
        _channel.Verify(c => c.BasicAckAsync(DeliveryTag, false, It.IsAny<CancellationToken>()), Times.Once);
        _channel.Verify(c => c.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private void VerifyNacked(bool requeue)
    {
        _channel.Verify(c => c.BasicNackAsync(DeliveryTag, false, requeue, It.IsAny<CancellationToken>()), Times.Once);
        _channel.Verify(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
