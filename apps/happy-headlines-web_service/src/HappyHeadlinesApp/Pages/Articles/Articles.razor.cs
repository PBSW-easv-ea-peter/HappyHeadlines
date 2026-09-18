using HappyHeadlinesApp.Models;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace HappyHeadlinesApp.Pages.Articles;

public partial class Articles : ComponentBase
{
    [Parameter]
    public string? Region { get; set; }

    private static readonly string[] Regions =
    [
        "eu",
        "na",
        "sa",
        "as",
        "af",
        "au",
        "an",
        "go"
    ];

    private IEnumerable<Article>? articles;
    private bool isLoading = true;

    protected override async Task OnParametersSetAsync()
    {
        isLoading = true;
        articles = [];

        var regionsToLoad = Region is null
            ? Regions
            : [Region];

        var regionTasks = regionsToLoad.Select(LoadRegionAsync).ToList();

        while (regionTasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(regionTasks);

            regionTasks.Remove(completedTask);

            var loadedArticles = await completedTask;

            if (loadedArticles.Count > 0)
            {
                articles = articles
                    .Concat(loadedArticles)
                    .ToList();

                await InvokeAsync(StateHasChanged);
            }
        }

        isLoading = false;
    }

    private async Task<List<Article>> LoadRegionAsync(string region)
    {
        var regionCode = region.ToUpper();

        try
        {
            var articleDtos =
                await Http.GetFromJsonAsync<IEnumerable<ArticleDTO>>(
                    $"http://localhost:8080/api/articles/{regionCode}");

            if (articleDtos is null)
                return [];

            // Fetch comments for all articles in this region in parallel
            var commentTasks = articleDtos.Select(async articleDto =>
            {
                try
                {
                    return await Http.GetFromJsonAsync<IEnumerable<Comment>>(
                        $"http://localhost:8082/api/comments/{regionCode}/{articleDto.Id}")
                        ?? [];
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(
                        ex,
                        "Failed to load comments for article {ArticleId}",
                        articleDto.Id);

                    return Enumerable.Empty<Comment>();
                }
            });

            var commentResults = await Task.WhenAll(commentTasks);

            return articleDtos
                .Select((articleDto, index) => new Article
                {
                    Id = articleDto.Id,
                    JournalistName = articleDto.JournalistName,
                    Title = articleDto.Title,
                    CreatedDate = articleDto.CreatedDate,
                    Location = articleDto.Location,
                    SectionName = articleDto.SectionName,
                    BreadText = articleDto.BreadText,
                    Comments = commentResults[index]
                })
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                ex,
                "An exception occurred loading articles for region {Region}",
                regionCode);

            return [];
        }
    }

    private bool IsCurrentRegion(string region)
    {
        return string.Equals(
            Region,
            region,
            StringComparison.OrdinalIgnoreCase);
    }

    private void NavigateToRegion(string? region)
    {
        Navigation.NavigateTo(
            region is null
                ? "/articles"
                : $"/articles/{region}");
    }
}
