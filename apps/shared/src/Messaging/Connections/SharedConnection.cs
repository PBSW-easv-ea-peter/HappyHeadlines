using RabbitMQ.Client;

namespace Messaging.Connections;

// One connection per service instead of one per publish - opening a connection is a TCP and AMQP
// handshake, a channel is cheap. Register as a singleton. The connection is opened on first use,
// so the service starts while the broker is down, and reopened if it has been closed for good;
// short broker restarts are handled by the client's automatic recovery.
public sealed class SharedConnection(IConnectionFactory factory) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }
}
