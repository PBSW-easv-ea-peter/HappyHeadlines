using Microsoft.AspNetCore.Components;
using ReaderPages.web.Models;
using ReaderPages.web;
using Reader.web.wasm.Components;

namespace Reader.web.wasm.Pages;

public partial class FrontPage : ComponentBase
{
    private const int PopularCount = 5;
    private const int SectionCardCount = 3;

    [Inject]
    private IHttpClientFactory HttpClientFactory { get; set; } = default!;

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
    private string? _loadedKey;
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
        // Only what this page shows is fetched (GAF), so a new section is a new request:
        // the hero and the popular list then come from that section.
        var region = RegionCode;
        var key = $"{region}/{ActiveSection}";
        if (key == _loadedKey)
            return;

        _loadedKey = key;
        _isLoading = true;

        // The hero shows an excerpt; the popular list and the section cards show no body text.
        var articles = await ArticleCatalog.GetFrontPageAsync(HttpClientFactory, Logger, region,
            top: 1 + PopularCount,
            perSection: SectionCardCount,
            section: ActiveSection,
            excerpt: 0,
            heroExcerpt: HeroArticle.ExcerptLength);

        // The reader may have switched edition or section again while this was loading.
        if (key != _loadedKey)
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
