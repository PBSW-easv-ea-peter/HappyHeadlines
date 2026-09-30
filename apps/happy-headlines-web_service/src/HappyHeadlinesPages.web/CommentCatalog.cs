using HappyHeadlinesPages.web.Models;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace HappyHeadlinesPages.web;

// CommentService only returns approved comments from this endpoint, so the
// result is safe to show to readers as-is.
public static class CommentCatalog
{
    public static async Task<List<Comment>> GetApprovedAsync(HttpClient http, ILogger logger, string region, long articleId)
    {
        var regionCode = region.ToUpper();

        try
        {
            var comments = await http.GetFromJsonAsync<List<Comment>>(
                $"http://localhost:8082/api/comments/{regionCode}/{articleId}");

            return comments ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load comments for article {ArticleId} in region {Region}", articleId, regionCode);
            return [];
        }
    }
}
