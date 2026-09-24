using HappyHeadlinesPages.web.Models;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace HappyHeadlinesPages.web.Pages.Drafts;

public partial class NewDraft : ComponentBase
{
    private const string DraftServiceBaseUrl = "http://localhost:8083";

    private string title = string.Empty;
    private string breadtext = string.Empty;
    private string location = Locations.All[0].Code;
    private long sectionId = Sections.All[0].Id;
    private IReadOnlyCollection<long> creditedJournalistIds = [];
    private string byline = string.Empty;
    private bool isSaving;

    private void RegenerateByline()
    {
        byline = BylineFormatter.Generate(
            JournalistState.All.Where(j => creditedJournalistIds.Contains(j.Id)));
    }

    private void SetCreditedJournalistIds(IReadOnlyCollection<long> ids)
    {
        creditedJournalistIds = ids.ToHashSet();
    }

    private async Task CreateAsync()
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(breadtext))
        {
            Snackbar.Add("Title and body are required.", Severity.Warning);
            return;
        }

        isSaving = true;

        try
        {
            var response = await Http.PostAsJsonAsync(
                $"{DraftServiceBaseUrl}/api/drafts",
                new CreateDraftRequest
                {
                    JournalistId = JournalistState.JournalistId,
                    Title = title,
                    Breadtext = breadtext,
                    Location = location,
                    SectionId = sectionId,
                    CreditedJournalistIds = creditedJournalistIds.ToList(),
                    Byline = string.IsNullOrWhiteSpace(byline) ? null : byline
                });

            if (!response.IsSuccessStatusCode)
            {
                Snackbar.Add("Could not create the draft.", Severity.Error);
                return;
            }

            Snackbar.Add("Draft created.", Severity.Success);
            Navigation.NavigateTo(PageRoutes.Drafts);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to create draft");
            Snackbar.Add("Could not create the draft.", Severity.Error);
        }
        finally
        {
            isSaving = false;
        }
    }
}
