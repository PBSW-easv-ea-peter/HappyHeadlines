using Messaging.Connections;
using Moq;
using RabbitMQ.Client;
using Xunit;

namespace PublishService.Tests.Shared;

public class SharedConnectionTests
{
    private readonly Mock<IConnectionFactory> _factory = new();

    private static Mock<IConnection> Connection(bool isOpen)
    {
        var connection = new Mock<IConnection>();
        connection.SetupGet(c => c.IsOpen).Returns(isOpen);
        return connection;
    }

    [Fact]
    public async Task GetAsync_ReusesAnOpenConnection()
    {
        var open = Connection(isOpen: true);
        _factory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(open.Object);
        var shared = new SharedConnection(_factory.Object);

        var first = await shared.GetAsync();
        var second = await shared.GetAsync();

        Assert.Same(first, second);
        _factory.Verify(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ReplacesAClosedConnection()
    {
        var closed = Connection(isOpen: false);
        var open = Connection(isOpen: true);
        _factory.SetupSequence(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(closed.Object)
            .ReturnsAsync(open.Object);
        var shared = new SharedConnection(_factory.Object);

        await shared.GetAsync();
        var replacement = await shared.GetAsync();

        Assert.Same(open.Object, replacement);
        closed.Verify(c => c.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAsync_BrokerDown_ThrowsAndTriesAgainNextTime()
    {
        var open = Connection(isOpen: true);
        _factory.SetupSequence(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broker down"))
            .ReturnsAsync(open.Object);
        var shared = new SharedConnection(_factory.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => shared.GetAsync());
        var connection = await shared.GetAsync();

        Assert.Same(open.Object, connection);
    }
}
