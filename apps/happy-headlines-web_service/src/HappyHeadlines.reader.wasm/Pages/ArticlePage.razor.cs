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

    private ArticleDTO? _article;
    private List<Comment> _comments = [];
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
        await Task.WhenAll(articleTask, commentsTask);

        // The reader may have navigated to another article while this was loading.
        if (key != _loadedKey)
            return;

        _article = articleTask.Result;
        _comments = commentsTask.Result.OrderBy(c => c.CreatedDate).ToList();
        _isLoading = false;
    }
}
