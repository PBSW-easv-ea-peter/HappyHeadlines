using PublishService.Models;

namespace PublishService.Services.External;

public class DraftClient(
    HttpClient httpClient) 
    : IDraftService
{
    public async Task<DraftDTO?> GetDraftAsync(Guid id)
    {
        var response = await httpClient.GetAsync($"api/drafts/filled-in-draft/{id}");

        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<DraftDTO>();
    }
}