using Polly.CircuitBreaker;

namespace DraftService.Profanity;

public class ProfanityClient : IProfanityClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProfanityClient> _logger;

    public ProfanityClient(HttpClient httpClient, ILogger<ProfanityClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProfanityCheckResult> CheckAsync(string text, CancellationToken cancellationToken = default)
    {
        List<string> bannedWords = [];
        bool circuitIsOpen = false;

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/profanity/check",
                new { Text = text },
                cancellationToken);

            response.EnsureSuccessStatusCode();

            bannedWords = await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken: cancellationToken)
                ?? [];
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("ProfanityService circuit is open - skipping profanity check");
            circuitIsOpen = true;
        }
        catch (HttpRequestException)
        {
            _logger.LogWarning("ProfanityService got a HttpRequestException");
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("TaskCanceledException was thrown.");
        }

        return new ProfanityCheckResult(bannedWords, circuitIsOpen);
    }
}
