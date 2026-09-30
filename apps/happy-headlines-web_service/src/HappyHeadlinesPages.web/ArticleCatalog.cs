using HappyHeadlinesPages.web.Models;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace HappyHeadlinesPages.web;

// ArticleService shards articles by region with no "all articles" or
// "by journalist" endpoint, so callers that need the full catalog fan out
// across every region client-side, same as Articles.razor.cs does.
public static class ArticleCatalog
{
    public static readonly string[] Regions =
    [
        "eu", "na", "sa", "as", "af", "au", "an", "go"
    ];

    public static async Task<List<ArticleDTO>> GetAllArticlesAsync(HttpClient http, ILogger logger)
    {
        var regionResults = await Task.WhenAll(Regions.Select(region => GetRegionArticlesAsync(http, logger, region)));

        return regionResults.SelectMany(articles => articles).ToList();
    }

    public static async Task<List<ArticleDTO>> GetRegionArticlesAsync(HttpClient http, ILogger logger, string region)
    {
        var regionCode = region.ToUpper();

        try
        {
            var articles = await http.GetFromJsonAsync<List<ArticleDTO>>(
                $"http://localhost:8080/api/articles/{regionCode}");

            return articles ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load articles for region {Region}", regionCode);
            return [];
        }
    }

    // Ids are only unique within a shard, so the region is part of the key.
    // Returns null both for a 404 and for a failed call - callers show "not found".
    public static async Task<ArticleDTO?> GetArticleAsync(HttpClient http, ILogger logger, string region, long id)
    {
        var regionCode = region.ToUpper();

        try
        {
            return await http.GetFromJsonAsync<ArticleDTO>(
                $"http://localhost:8080/api/articles/{regionCode}/{id}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load article {ArticleId} for region {Region}", id, regionCode);
            return null;
        }
    }
}
