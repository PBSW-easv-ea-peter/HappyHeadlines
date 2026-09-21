using Polly;
using Polly.Registry;
using Polly.CircuitBreaker;

namespace CommentService.Profanity;

public class ProfanityClient : IProfanityClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProfanityClient> _logger;
//    private readonly ResiliencePipeline _pipeline;

    public ProfanityClient(
            HttpClient httpClient,
            ILogger<ProfanityClient> logger)
//            ResiliencePipelineProvider<string> pipelineProvider)
    {
        _httpClient = httpClient;
        _logger = logger;
//        _pipeline = pipelineProvider.GetPipeline("ProfanityService");
    }

    public async Task<ProfanityCheckResult> CheckAsync(string text, CancellationToken cancellationToken = default)
    {
        List<string> bannedWords = [];
        bool circuitIsOpen = false;

        try
        {
            _logger.LogInformation($"Checking for banned words: {text}");
            var response = await _httpClient.PostAsJsonAsync(
                    "api/profanity/check",
                    new { Text = text },
                    cancellationToken);
            response.EnsureSuccessStatusCode();
            bannedWords = await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken: cancellationToken)
                ?? [];
//            bannedWords = await _pipeline.ExecuteAsync(async ct =>
//            {
//                var response = await _httpClient.PostAsJsonAsync("api/profanity/check", new { Text = text }, ct);
//                response.EnsureSuccessStatusCode();
//                return await response.Content.ReadFromJsonAsync<List<string>>(cancellationToken: ct)
//                    ?? [];
//            }, cancellationToken);
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
            _logger.LogWarning("TaskCanceledException was throw.");
        }
        finally
        {
            _logger.LogInformation($"Returned output from ProfanityService: {string.Join(", ", bannedWords)}, {circuitIsOpen}");
        
        }

        return new ProfanityCheckResult(bannedWords, circuitIsOpen);
    }
}
