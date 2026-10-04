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
            logger.LogInformation("Draft not found for id: {id}", id);
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
            logger.LogWarning("DraftService refused to mark draft {DraftId} as published: {Reason}",
                id, await response.Content.ReadAsStringAsync());
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }
}
