using System.Net.Http.Json;
using HappyHeadlinesPages.web.Models;
using Microsoft.Extensions.Logging;

namespace HappyHeadlinesPages.web;

// "Signed in as" picker in the nav and the Drafts page always agree on who's
// selected. The journalist list itself now comes from DraftService, which
// owns Journalist - there is no login system yet, so this just loads the
// real roster once and defaults to the first entry.
public class CurrentJournalistState
{
    private const string DraftServiceBaseUrl = "http://localhost:8083";

    private readonly HttpClient _http;
    private readonly ILogger<CurrentJournalistState> _logger;

    public CurrentJournalistState(HttpClient http, ILogger<CurrentJournalistState> logger)
    {
        _http = http;
        _logger = logger;
    }

    public IReadOnlyList<Journalist> All { get; private set; } = [];

    public bool IsLoaded { get; private set; }

    public long JournalistId { get; private set; }

    public event Action? OnChange;

    public async Task EnsureLoadedAsync()
    {
        if (IsLoaded)
            return;

        try
        {
            var journalists = await _http.GetFromJsonAsync<List<Journalist>>(
                $"{DraftServiceBaseUrl}/api/journalists");

            All = (journalists ?? [])
                .OrderBy(j => LastName(j.Name))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load journalists from DraftService");
            All = [];
        }

        if (All.Count > 0)
            JournalistId = All[0].Id;

        IsLoaded = true;
        OnChange?.Invoke();
    }

    public string NameOf(long id) =>
        All.FirstOrDefault(j => j.Id == id)?.Name ?? $"Journalist #{id}";

    public void SetJournalist(long journalistId)
    {
        if (JournalistId == journalistId)
            return;

        JournalistId = journalistId;
        OnChange?.Invoke();
    }

    private static string LastName(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[^1] : name;
    }
}
