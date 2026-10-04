using Polly.CircuitBreaker;

namespace CommentService.Profanity;

public class ProfanityClient : IProfanityClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProfanityClient> _logger;

    public ProfanityClient(HttpClient httpClient, ILogger<ProfanityClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // Fails closed: anything other than a successful answer from ProfanityService is
    // reported as Unavailable, never as "no banned words".
    public async Task<ProfanityCheckResult> CheckAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                    "api/profanity/check",
                    new { Text = text },
                    cancellationToken);
            response.EnsureSuccessStatusCode();

            var bannedWords = await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken: cancellationToken);
            if (bannedWords is null)
            {
                _logger.LogWarning("ProfanityService returned an empty response - treating the text as not checked");
                return ProfanityCheckResult.NotChecked;
            }

            // Only the count - the banned words themselves must not be logged (docs/logging.md).
            _logger.LogInformation("Profanity check finished with {BannedWordCount} banned words", bannedWords.Count);
            return new ProfanityCheckResult(bannedWords, Unavailable: false);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("ProfanityService circuit is open - skipping profanity check");
            return ProfanityCheckResult.NotChecked;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Unreachable, timed out (TaskCanceledException from HttpClient.Timeout), error
            // status or unreadable body. Exception type only - the message can contain URLs.
            _logger.LogWarning("ProfanityService call failed ({ExceptionType}) - treating the text as not checked",
                ex.GetType().Name);
            return ProfanityCheckResult.NotChecked;
        }
    }
}
