using Polly.CircuitBreaker;
using PublishService.Shared.External;

namespace PublishService.Shared.HttpClients;

public class ProfanityClient(
    HttpClient httpClient,
    ILogger<ProfanityClient> logger)
    : IProfanityClient
{
    // Fails closed: anything other than a successful answer from ProfanityService is reported
    // as Unavailable, never as "no banned words".
    public async Task<ProfanityCheckResult> CheckAsync(string text)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/profanity/check", new { Text = text });

            response.EnsureSuccessStatusCode();

            var bannedWords = await response.Content.ReadFromJsonAsync<List<string>>();
            if (bannedWords is null)
            {
                logger.LogWarning("ProfanityService returned an empty response - treating the text as not checked");
                return ProfanityCheckResult.NotChecked;
            }

            return new ProfanityCheckResult(bannedWords, Unavailable: false);
        }
        catch (BrokenCircuitException)
        {
            logger.LogWarning("ProfanityService circuit is open - the text cannot be checked");
            return ProfanityCheckResult.NotChecked;
        }
        catch (Exception ex)
        {
            // Unreachable, timed out, error status or unreadable body. Exception type only -
            // the message can contain URLs.
            logger.LogWarning("Call to {Pipeline} failed - treating the text as not checked. Reason: {Reason}",
                "ProfanityService", ex.GetType().Name);
            return ProfanityCheckResult.NotChecked;
        }
    }
}
