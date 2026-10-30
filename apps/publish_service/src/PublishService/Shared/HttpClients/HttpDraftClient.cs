using System.Net;
using PublishService.Shared.External;
using PublishService.Shared.Models;

namespace PublishService.Shared.HttpClients;

public class HttpDraftClient(
    HttpClient httpClient,
    ILogger<HttpDraftClient> logger) 
    : IHttpDraftClient
{
    public async Task<DraftDTO?> GetAsync(Guid id)
    {
        var response = await httpClient.GetAsync($"api/drafts/filled-in-draft/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation("Draft {DraftId} not found", id);
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<DraftDTO>();
    }

    public async Task<bool> MarkPublishedAsync(Guid id)
    {
        var response = await httpClient.PostAsync($"api/drafts/{id}/publish", content: null);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            // Not the response body: Reason is the exception type only (docs/logging.md).
            logger.LogWarning("DraftService refused to mark draft {DraftId} as published", id);
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<bool> FlagProfanityAsync(Guid id, IReadOnlyList<string> flaggedWords)
    {
        var response = await httpClient.PutAsJsonAsync(
            $"api/drafts/{id}/flagged-words",
            new { FlaggedWords = flaggedWords });

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            logger.LogWarning("DraftService refused to flag draft {DraftId}", id);
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }
}
