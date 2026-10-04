using System.Net.Sockets;
using ArticleService.Setup;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Polly;
using Polly.CircuitBreaker;
using Xunit;

namespace ArticleService.Tests.Resilience;

// Verifies which failures count as "shard unreachable" and open the circuit, and which
// are poison/permanent and must pass straight through without tripping it.
public class ShardResilienceTests
{
    public static TheoryData<Exception> ShardUnreachable => new()
    {
        // What a stopped shard container gives in Docker: DNS lookup fails, Npgsql does not wrap it.
        new SocketException((int)SocketError.HostNotFound),
        new NpgsqlException("Connection refused", new SocketException()),
        new TimeoutException()
    };

    public static TheoryData<Exception> NotShardFailure => new()
    {
        // Unknown section (poison message).
        new ArgumentException("Unknown section 'Sport'."),
        // E.g. a constraint violation - retrying will never succeed.
        new NpgsqlException("Permanent failure")
    };

    [Theory]
    [MemberData(nameof(ShardUnreachable))]
    public async Task ShardUnreachable_OpensCircuit(Exception failure)
    {
        var failures = await ExecuteFailingThreeTimes(failure);

        Assert.Contains(failures, ex => ex is BrokenCircuitException);
    }

    [Theory]
    [MemberData(nameof(NotShardFailure))]
    public async Task NotShardFailure_DoesNotRetryOrOpenCircuit(Exception failure)
    {
        var attempts = 0;
        var failures = await ExecuteFailingThreeTimes(failure, () => attempts++);

        Assert.DoesNotContain(failures, ex => ex is BrokenCircuitException);
        Assert.Equal(3, attempts);
    }

    private static async Task<List<Exception>> ExecuteFailingThreeTimes(Exception failure, Action? onAttempt = null)
    {
        var builder = new ResiliencePipelineBuilder();
        ShardResilience.Configure(builder, ShardResilience.PipelineKey("EU"), NullLogger.Instance);
        var pipeline = builder.Build();

        var failures = new List<Exception>();
        for (var i = 0; i < 3; i++)
        {
            try
            {
                await pipeline.ExecuteAsync<bool>(_ =>
                {
                    onAttempt?.Invoke();
                    throw failure;
                });
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        }

        return failures;
    }
}
