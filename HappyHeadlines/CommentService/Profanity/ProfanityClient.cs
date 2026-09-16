using System.Net.Http.Json;
using Polly;
using Polly.CircuitBreaker;

namespace CommentService.Profanity;

// This is the one HTTP call in the whole system that crosses a swim lane boundary
// synchronously (CommentService -> ProfanityService, direct, no gateway - required by this
// week's assignment). Chapter 21's principle 2 says nothing should cross a swim lane
// boundary synchronously; since the assignment rules out a queue here, the retry + circuit
// breaker pair below is the compensating control that keeps a struggling ProfanityService
// from taking CommentService down with it, matching the VMware failure-isolation article's
// guidance for exactly this situation.
public class ProfanityClient : IProfanityClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProfanityClient> _logger;
    private readonly IAsyncPolicy _resiliencePolicy;

    public ProfanityClient(HttpClient httpClient, ILogger<ProfanityClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Retry twice with a short backoff to ride out a transient blip...
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(400) });

        // ...then, after 3 failures in a row, trip the circuit for 30 seconds so
        // ProfanityService gets room to recover instead of being hammered while it is down.
        var circuitBreakerPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, breakDelay) =>
                    _logger.LogWarning(exception, "ProfanityService circuit breaker tripped for {BreakDelay}", breakDelay),
                onReset: () => _logger.LogInformation("ProfanityService circuit breaker reset"));

        _resiliencePolicy = retryPolicy.WrapAsync(circuitBreakerPolicy);
    }

    public async Task<ProfanityCheckResult> CheckAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var bannedWords = await _resiliencePolicy.ExecuteAsync(async ct =>
            {
                var response = await _httpClient.PostAsJsonAsync("api/profanity/check", new { Text = text }, ct);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken: ct)
                    ?? [];
            }, cancellationToken);

            return new ProfanityCheckResult(BannedWords: bannedWords, CircuitOpen: false);
        }
        catch (BrokenCircuitException exception)
        {
            _logger.LogWarning(exception, "ProfanityService circuit is open - skipping profanity check");
            return new ProfanityCheckResult(BannedWords: [], CircuitOpen: true);
        }
    }
}
