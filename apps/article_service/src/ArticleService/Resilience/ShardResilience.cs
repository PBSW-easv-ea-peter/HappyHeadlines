using System.Net.Sockets;
using Npgsql;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace ArticleService.Resilience;

// One retry + circuit breaker pipeline per article shard, so a failing shard (e.g. EU)
// fails fast without affecting writes to the other shards.
public static class ShardResilience
{
    public static string PipelineKey(string location) => $"ArticleShard:{location.ToUpperInvariant()}";

    public static WebApplicationBuilder AddShardResilience(this WebApplicationBuilder builder)
    {
        var locations = builder.Configuration.GetSection("ArticleShards").GetChildren().Select(s => s.Key);

        foreach (var location in locations)
        {
            var key = PipelineKey(location);

            builder.Services.AddResiliencePipeline(key, (pipeline, context) =>
            {
                // Circuit state changes go through ILogger so they reach Loki (docs/logging.md).
                var logger = context.ServiceProvider
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("ArticleShard.CircuitBreaker");

                Configure(pipeline, key, logger);
            });
        }

        return builder;
    }

    // Internal so ArticleService.Tests can verify which failures open the circuit.
    internal static void Configure(ResiliencePipelineBuilder pipeline, string key, ILogger logger)
    {
        // Only "shard unreachable" failures are worth retrying; e.g. a constraint
        // violation is also an NpgsqlException but will never succeed.
        // SocketException: Npgsql does not wrap DNS failures, which is what a stopped
        // shard container gives in Docker.
        var shouldHandle = new PredicateBuilder()
            .Handle<NpgsqlException>(ex => ex.IsTransient)
            .Handle<SocketException>()
            .Handle<TimeoutException>();

        pipeline.AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = shouldHandle,

            // Kept short: the consumer handles one message at a time, so retries
            // here delay messages for the other shards too.
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential
        });

        pipeline.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            // Two failed operations out of two -> open circuit.
            FailureRatio = 1.0,
            MinimumThroughput = 2,

            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(30),

            ShouldHandle = shouldHandle,

            OnOpened = args =>
            {
                // Exception type only - the message can contain connection details.
                logger.LogWarning(
                    "Circuit {Pipeline} opened. Reason: {Reason}",
                    key, args.Outcome.Exception?.GetType().Name);

                return ValueTask.CompletedTask;
            },

            OnClosed = args =>
            {
                logger.LogInformation("Circuit {Pipeline} closed. Writes will resume.", key);

                return ValueTask.CompletedTask;
            },

            OnHalfOpened = args =>
            {
                logger.LogInformation("Circuit {Pipeline} half-open. Next write is a trial.", key);

                return ValueTask.CompletedTask;
            }
        });
    }
}
