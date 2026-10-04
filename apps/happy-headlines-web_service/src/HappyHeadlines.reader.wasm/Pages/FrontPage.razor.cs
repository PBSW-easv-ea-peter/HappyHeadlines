using HappyHeadlinesPages.web;
using HappyHeadlinesPages.web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace HappyHeadlines.reader.wasm.Pages;

public partial class FrontPage : ComponentBase
{
    private const int PopularCount = 5;
    private const int SectionCardCount = 3;

    [Inject]
    private HttpClient Http { get; set; } = default!;

    [Inject]
    private ILogger<FrontPage> Logger { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "region")]
    public string? Region { get; set; }

    [SupplyParameterFromQuery(Name = "section")]
    public string? Section { get; set; }

    // Newest first - doubles as the "popularity" order until ArticleService
    // has real view data.
    private List<ArticleDTO> _articles = [];
    private string? _loadedRegion;
    private bool _isLoading = true;

    private string RegionCode => ReaderRegions.Normalize(Region);

    private string? ActiveSection =>
        Sections.All
            .Select(s => s.Name)
            .FirstOrDefault(name => string.Equals(name, Section, StringComparison.OrdinalIgnoreCase));

    private IEnumerable<ArticleDTO> Visible =>
        ActiveSection is null ? _articles : _articles.Where(a => IsInSection(a, ActiveSection));

    private ArticleDTO? Highlighted => Visible.FirstOrDefault();

    private List<ArticleDTO> Popular => Visible.Skip(1).Take(PopularCount).ToList();

    protected override async Task OnParametersSetAsync()
    {
        // Switching section only re-filters what is already loaded.
        var region = RegionCode;
        if (region == _loadedRegion)
            return;

        _loadedRegion = region;
        _isLoading = true;

        var articles = await ArticleCatalog.GetRegionArticlesAsync(Http, Logger, region);

        // The reader may have switched edition again while this was loading.
        if (region != _loadedRegion)
            return;

        _articles = articles.OrderByDescending(ArticleDisplay.PublishedAt).ToList();
        _isLoading = false;
    }

    private List<ArticleDTO> InSection(string name) =>
        _articles.Where(a => IsInSection(a, name)).Take(SectionCardCount).ToList();

    private static bool IsInSection(ArticleDTO article, string name) =>
        string.Equals(article.SectionName, name, StringComparison.OrdinalIgnoreCase);

    // null removes the parameter, i.e. back to "All".
    private string SectionHref(string? section) =>
        Navigation.GetUriWithQueryParameter("section", section);

    private static string ChipStyle(string color, bool isActive) => isActive
        ? $"background-color:{color};border-color:{color};color:#fff;"
        : $"border-color:{color};color:{color};";
}
