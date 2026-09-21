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
        var regionResults = await Task.WhenAll(Regions.Select(region => LoadRegionAsync(http, logger, region)));

        return regionResults.SelectMany(articles => articles).ToList();
    }

    private static async Task<List<ArticleDTO>> LoadRegionAsync(HttpClient http, ILogger logger, string region)
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
}
