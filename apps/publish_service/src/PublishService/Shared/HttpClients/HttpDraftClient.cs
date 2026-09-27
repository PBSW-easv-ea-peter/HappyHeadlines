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
        var response = await httpClient.GetAsync($"api/drafts/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation("Draft not found for id: {id}", id);
            return null;
        }
        
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<DraftDTO>();
    }
}