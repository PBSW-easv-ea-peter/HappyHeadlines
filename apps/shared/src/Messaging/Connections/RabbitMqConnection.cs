using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Messaging.Connections;

public static class RabbitMqConnection
{
    private static readonly TimeSpan FirstDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(30);

    // Keeps trying until the broker answers or the token is cancelled, doubling the delay up to
    // 30 s. A service that starts before RabbitMQ (Swarm ignores depends_on) waits instead of
    // crashing. Once connected, the client's automatic recovery handles later broker restarts.
    public static async Task<IConnection> CreateWithRetryAsync(
        IConnectionFactory factory,
        Action<Exception> onRetry,
        CancellationToken cancellationToken = default)
    {
        var delay = FirstDelay;
        while (true)
        {
            try
            {
                return await factory.CreateConnectionAsync(cancellationToken);
            }
            catch (BrokerUnreachableException ex)
            {
                onRetry(ex);
                await Task.Delay(delay, cancellationToken);
                delay = delay * 2 < MaxDelay ? delay * 2 : MaxDelay;
            }
        }
    }
}
