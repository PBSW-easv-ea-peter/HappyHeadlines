using HappyHeadlinesPages.web.Models;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace HappyHeadlinesPages.web.Pages.Drafts;

public partial class Drafts : ComponentBase
{
    private const string DraftServiceBaseUrl = "http://localhost:8083";

    private List<Draft> _allDrafts = [];
    private List<ArticleDTO> _allArticles = [];
    private bool isLoading = true;

    private long SelectedJournalistId => JournalistState.JournalistId;

    private IEnumerable<Draft> MyDrafts =>
        _allDrafts.Where(d => d.CreatedByJournalistId == SelectedJournalistId);

    private IEnumerable<Draft> MyActiveDrafts =>
        MyDrafts.Where(d => d.Status != DraftStatus.Published && d.Status != DraftStatus.Archived);

    private IEnumerable<Draft> MyArchivedDrafts =>
        MyDrafts.Where(d => d.Status == DraftStatus.Archived);

    // DraftService.Publish never actually creates an Article, so "published" is
    // sourced from ArticleService directly - matched by journalist name, since
    // Article responses don't carry a journalist id, only the name.
    private IEnumerable<ArticleDTO> MyPublishedArticles =>
        _allArticles.Where(a => string.Equals(
            a.Byline,
            JournalistState.NameOf(SelectedJournalistId),
            StringComparison.OrdinalIgnoreCase));

    private IEnumerable<Draft> PendingApprovalDrafts =>
        _allDrafts.Where(d => d.Status == DraftStatus.PendingApproval);

    private int MyDraftCount => MyDrafts.Count(d =>
        d.Status == DraftStatus.WorkInProgress && string.IsNullOrWhiteSpace(d.ReviewNote));

    private int MyNeedsChangesCount => MyDrafts.Count(d =>
        d.Status == DraftStatus.WorkInProgress && !string.IsNullOrWhiteSpace(d.ReviewNote));

    private int MyPublishedCount => MyPublishedArticles.Count();
    private int AwaitingReviewCount => PendingApprovalDrafts.Count();

    private string Greeting
    {
        get
        {
            var firstName = JournalistState.NameOf(SelectedJournalistId).Split(' ')[0];
            return $"Good {Greetings.TimeOfDay()}, {firstName}";
        }
    }

    protected override async Task OnInitializedAsync()
    {
        JournalistState.OnChange += OnJournalistChanged;
        await Task.WhenAll(LoadDraftsAsync(), LoadArticlesAsync());
    }

    private void OnJournalistChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        JournalistState.OnChange -= OnJournalistChanged;
    }

    private async Task LoadDraftsAsync()
    {
        isLoading = true;

        try
        {
            var drafts = await Http.GetFromJsonAsync<List<Draft>>($"{DraftServiceBaseUrl}/api/drafts");
            _allDrafts = drafts ?? [];
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load drafts from DraftService");
            _allDrafts = [];
        }

        isLoading = false;
    }

    private async Task LoadArticlesAsync()
    {
        _allArticles = await ArticleCatalog.GetAllArticlesAsync(Http, Logger);
    }

    private async Task ReactivateAsync(long draftId)
    {
        try
        {
            var response = await Http.PostAsync($"{DraftServiceBaseUrl}/api/drafts/{draftId}/reactivate", null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to reactivate draft {DraftId}", draftId);
        }

        await LoadDraftsAsync();
    }
}
