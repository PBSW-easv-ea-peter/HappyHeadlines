using HappyHeadlinesPages.web;
using HappyHeadlinesPages.web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace HappyHeadlines.reader.wasm.Pages;

public partial class ArticlePage : ComponentBase
{
    [Inject]
    private HttpClient Http { get; set; } = default!;

    [Inject]
    private ILogger<ArticlePage> Logger { get; set; } = default!;

    [Parameter]
    public long Id { get; set; }

    // The article's shard - ids are only unique within one region.
    [SupplyParameterFromQuery(Name = "region")]
    public string? Region { get; set; }

    private const int MoreNewsCount = 4;

    private ArticleDTO? _article;
    private List<Comment> _comments = [];
    private List<ArticleDTO> _moreNews = [];
    private string? _loadedKey;
    private bool _isLoading = true;

    private string RegionCode => ReaderRegions.Normalize(Region);

    private string FrontPageHref =>
        RegionCode == ReaderRegions.Default ? "./" : $"./?region={RegionCode}";

    private IEnumerable<string> Paragraphs =>
        (_article?.BreadText ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    protected override async Task OnParametersSetAsync()
    {
        var region = RegionCode;
        var key = $"{region}/{Id}";
        if (key == _loadedKey)
            return;

        _loadedKey = key;
        _isLoading = true;

        var articleTask = ArticleCatalog.GetArticleAsync(Http, Logger, region, Id);
        var commentsTask = CommentCatalog.GetApprovedAsync(Http, Logger, region, Id);
        var regionTask = ArticleCatalog.GetRegionArticlesAsync(Http, Logger, region);
        await Task.WhenAll(articleTask, commentsTask, regionTask);

        // The reader may have navigated to another article while this was loading.
        if (key != _loadedKey)
            return;

        _article = articleTask.Result;
        _comments = commentsTask.Result.OrderBy(c => c.CreatedDate).ToList();
        _moreNews = PickMoreNews(regionTask.Result, _article);
        _isLoading = false;
    }

    // Same edition, same section first, then the newest of the rest - so the
    // reader always has something to continue with.
    private List<ArticleDTO> PickMoreNews(List<ArticleDTO> regionArticles, ArticleDTO? current) =>
        regionArticles
            .Where(a => a.Id != Id)
            .OrderByDescending(a => current is not null
                && string.Equals(a.SectionName, current.SectionName, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(ArticleDisplay.PublishedAt)
            .Take(MoreNewsCount)
            .ToList();
}
