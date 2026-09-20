using HappyHeadlinesPages.web.Layout;
using HappyHeadlinesPages.web.Models;
using Microsoft.AspNetCore.Components;

namespace HappyHeadlinesPages.web.Pages;

public partial class Home : ComponentBase
{
    private static readonly string[] RainbowColors =
    [
        CustomMudTheme.LogoBlue,
        CustomMudTheme.LogoGreen,
        CustomMudTheme.LogoYellow,
        CustomMudTheme.LogoOrange,
        CustomMudTheme.LogoPink,
        CustomMudTheme.LogoPurple
    ];

    private List<ArticleDTO> _recentArticles = [];
    private bool isLoading = true;

    private ArticleDTO? FeaturedArticle => _recentArticles.FirstOrDefault();

    private IEnumerable<ArticleDTO> OtherArticles => _recentArticles.Skip(1);

    private string FirstName =>
        Journalists.NameOf(JournalistState.JournalistId).Split(' ')[0];

    private static string HeroBackgroundStyle =>
        $"background: linear-gradient(120deg, {string.Join(", ", RainbowColors.Select(c => c + "1A"))});";

    private static string RainbowBarStyle =>
        $"width:140px;height:5px;border-radius:3px;margin:0 auto 16px;" +
        $"background: linear-gradient(90deg, {string.Join(", ", RainbowColors)});";

    protected override async Task OnInitializedAsync()
    {
        JournalistState.OnChange += OnJournalistChanged;

        isLoading = true;

        var articles = await ArticleCatalog.GetAllArticlesAsync(Http, Logger);

        _recentArticles = articles
            .OrderByDescending(a => a.CreatedDate)
            .Take(6)
            .ToList();

        isLoading = false;
    }

    private void OnJournalistChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        JournalistState.OnChange -= OnJournalistChanged;
    }

    private void GoToArticle(ArticleDTO article) =>
        Navigation.NavigateTo($"/articles/{article.Location}");
}
