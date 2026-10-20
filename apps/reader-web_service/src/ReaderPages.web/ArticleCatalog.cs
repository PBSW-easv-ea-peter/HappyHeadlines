using System.Net.Http.Json;
using ReaderPages.web.Models;
using Microsoft.Extensions.Logging;

namespace ReaderPages.web;

// ArticleService shards articles by region with no "all articles" or
// "by journalist" endpoint, so callers that need the full catalog fan out
// across every region client-side, same as Articles.razor.cs does.
public static class ArticleCatalog
{
    public static readonly string[] Regions =
    [
        "eu", "na", "sa", "as", "af", "au", "an", "go"
    ];

    public static async Task<List<ArticleDTO>> GetAllArticlesAsync(IHttpClientFactory httpClientFactory, ILogger logger)
    {
        var regionResults = await Task.WhenAll(Regions.Select(region => GetRegionArticlesAsync(httpClientFactory, logger, region)));

        return regionResults.SelectMany(articles => articles).ToList();
    }

    public static async Task<List<ArticleDTO>> GetRegionArticlesAsync(IHttpClientFactory httpClientFactory, ILogger logger, string region)
    {
        var regionCode = region.ToUpper();

        try
        {
            var client = httpClientFactory.CreateClient("ArticleService");
            var articles = await client.GetFromJsonAsync<List<ArticleDTO>>(
                $"/api/articles/{regionCode}");

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
    public static async Task<ArticleDTO?> GetArticleAsync(IHttpClientFactory httpClientFactory, ILogger logger, string region, Guid id)
    {
        var regionCode = region.ToUpper();

        try
        {
            var client = httpClientFactory.CreateClient("ArticleService");
            return await client.GetFromJsonAsync<ArticleDTO>(
                $"/api/articles/{regionCode}/{id}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load article {ArticleId} for region {Region}", id, regionCode);
            return null;
        }
    }
}
