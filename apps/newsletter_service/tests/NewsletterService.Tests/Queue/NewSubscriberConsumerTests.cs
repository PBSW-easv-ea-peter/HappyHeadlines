using System.Text;
using System.Text.Json;
using Messaging.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NewsletterService.Queues;
using NewsletterService.Queues.NewSubscriber;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NewsletterService.Tests.Queue;

public class NewSubscriberConsumerTests
{
    private const ulong DeliveryTag = 42;

    private readonly Mock<IChannel> _channel = new();
    private readonly Mock<IMessageHandler<NewSubscriberEvent>> _messageHandler = new();
    private readonly NewSubscriberConsumer _consumer;

    public NewSubscriberConsumerTests()
    {
        var services = new ServiceCollection();
        
        // Register the mocked message handler
        services.AddSingleton(_messageHandler.Object);
        
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        _consumer = new NewSubscriberConsumer(
            new ConnectionFactory(), 
            scopeFactory, 
            NullLogger<NewSubscriberConsumer>.Instance);
    }

    [Fact]
    public async Task MessageHandler_Called_WhenValidMessageReceived()
    {
        // Arrange
        _messageHandler.Setup(h => h.HandleAsync(It.IsAny<NewSubscriberEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _channel.Setup(c => c.BasicAckAsync(DeliveryTag, false, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        // Act
        await _consumer.HandleMessageAsync(_channel.Object, Delivery(Event()), CancellationToken.None);

        // Assert
        _messageHandler.Verify(h => h.HandleAsync(It.IsAny<NewSubscriberEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleMessageAsync_ValidNewSubscriberEvent_Acks()
    {
        // Arrange
        _messageHandler.Setup(h => h.HandleAsync(It.IsAny<NewSubscriberEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _channel.Setup(c => c.BasicAckAsync(DeliveryTag, false, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        // Act
        await _consumer.HandleMessageAsync(_channel.Object, Delivery(Event()), CancellationToken.None);

        // Assert
        VerifyAcked();
    }

    [Fact]
    public async Task HandleMessageAsync_InvalidJson_NacksWithoutRequeue()
    {
        // Arrange
        var invalidBody = "invalid json";

        _channel.Setup(c => c.BasicNackAsync(DeliveryTag, false, false, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        // Act
        await _consumer.HandleMessageAsync(_channel.Object, Delivery(invalidBody), CancellationToken.None);

        // Assert
        _channel.Verify(c => c.BasicNackAsync(DeliveryTag, false, false, It.IsAny<CancellationToken>()), Times.Once);
        _channel.Verify(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleMessageAsync_HandlerThrowsException_NacksWithRequeue()
    {
        // Arrange
        _messageHandler.Setup(h => h.HandleAsync(It.IsAny<NewSubscriberEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        _channel.Setup(c => c.BasicNackAsync(DeliveryTag, false, true, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        // Act
        await _consumer.HandleMessageAsync(_channel.Object, Delivery(Event()), CancellationToken.None);

        // Assert
        _channel.Verify(c => c.BasicNackAsync(DeliveryTag, false, true, It.IsAny<CancellationToken>()), Times.Once);
        _channel.Verify(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static NewSubscriberEvent Event() => new()
    {
        Username = "Test User",
        Email = "test@example.com"
    };

    private static BasicDeliverEventArgs Delivery(NewSubscriberEvent newSubscriber) =>
        Delivery(JsonSerializer.Serialize(newSubscriber));

    private static BasicDeliverEventArgs Delivery(string body) => new(
        consumerTag: "test",
        deliveryTag: DeliveryTag,
        redelivered: false,
        exchange: "subscribers",
        routingKey: "subscriber.new",
        properties: new BasicProperties(),
        body: Encoding.UTF8.GetBytes(body));

    private void VerifyAcked()
    {
        _channel.Verify(c => c.BasicAckAsync(DeliveryTag, false, It.IsAny<CancellationToken>()), Times.Once);
        _channel.Verify(c => c.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
