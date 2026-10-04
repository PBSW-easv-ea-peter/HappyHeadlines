using HappyHeadlinesPages.web.Models;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace HappyHeadlinesPages.web.Pages.Drafts;

public partial class EditDraft : ComponentBase
{
    private const string DraftServiceBaseUrl = "http://localhost:8083";
    private const string PublishServiceBaseUrl = "http://localhost:8084";

    [Parameter]
    public Guid Id { get; set; }

    private Draft? _draft;
    private bool isLoading = true;
    private bool isSaving;

    private string title = string.Empty;
    private string breadtext = string.Empty;
    private string location = string.Empty;
    private long sectionId;
    private IReadOnlyCollection<long> creditedJournalistIds = [];
    private string byline = string.Empty;
    private string reviewNote = string.Empty;

    private void RegenerateByline()
    {
        byline = BylineFormatter.Generate(
            JournalistState.All.Where(j => creditedJournalistIds.Contains(j.Id)));
    }

    private void SetCreditedJournalistIds(IReadOnlyCollection<long> ids)
    {
        creditedJournalistIds = ids.ToHashSet();
    }

    private bool IsReadOnly => _draft is null || _draft.Status != DraftStatus.WorkInProgress;

    // Any status past WorkInProgress means the draft went through at least one
    // submit-for-approval, which is what triggers the profanity check.
    private bool HasBeenSubmitted => _draft is not null && _draft.Status != DraftStatus.WorkInProgress;

    private string PageTitle => _draft?.Status switch
    {
        DraftStatus.WorkInProgress => "Edit Draft",
        DraftStatus.PendingApproval => "Review Draft",
        _ => "View Draft"
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        isLoading = true;

        try
        {
            _draft = await Http.GetFromJsonAsync<Draft>($"{DraftServiceBaseUrl}/api/drafts/{Id}");

            if (_draft is not null)
            {
                title = _draft.Title;
                breadtext = _draft.Breadtext;
                location = _draft.Location;
                sectionId = _draft.SectionId;
                creditedJournalistIds = _draft.CreditedJournalists.Select(j => j.Id).ToHashSet();
                byline = _draft.Byline;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load draft {DraftId}", Id);
            _draft = null;
        }

        isLoading = false;
    }

    private async Task<bool> SaveAsync()
    {
        isSaving = true;

        try
        {
            var response = await Http.PutAsJsonAsync(
                $"{DraftServiceBaseUrl}/api/drafts/{Id}",
                new EditDraftRequest
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
                Snackbar.Add("Could not save changes.", Severity.Error);
                return false;
            }

            Snackbar.Add("Changes saved.", Severity.Success);
            await LoadAsync();
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to save draft {DraftId}", Id);
            Snackbar.Add("Could not save changes.", Severity.Error);
            return false;
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task SubmitForApprovalAsync()
    {
        if (!await SaveAsync())
            return;

        isSaving = true;

        try
        {
            var response = await Http.PostAsync($"{DraftServiceBaseUrl}/api/drafts/{Id}/submit-for-approval", null);

            if (!response.IsSuccessStatusCode)
            {
                Snackbar.Add("Could not submit the draft for approval.", Severity.Error);
                return;
            }

            Snackbar.Add("Submitted for approval.", Severity.Success);
            Navigation.NavigateTo(PageRoutes.Drafts);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to submit draft {DraftId} for approval", Id);
            Snackbar.Add("Could not submit the draft for approval.", Severity.Error);
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task ApproveAsync()
    {
        isSaving = true;

        try
        {
            var response = await Http.PostAsJsonAsync(
                $"{DraftServiceBaseUrl}/api/drafts/{Id}/approve",
                new ApproveDraftRequest { JournalistId = JournalistState.JournalistId, Note = reviewNote });

            if (!response.IsSuccessStatusCode)
            {
                Snackbar.Add("Could not approve the draft.", Severity.Error);
                return;
            }

            Snackbar.Add("Draft approved.", Severity.Success);
            Navigation.NavigateTo(PageRoutes.Drafts);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to approve draft {DraftId}", Id);
            Snackbar.Add("Could not approve the draft.", Severity.Error);
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task RejectAsync()
    {
        isSaving = true;

        try
        {
            var response = await Http.PostAsJsonAsync(
                $"{DraftServiceBaseUrl}/api/drafts/{Id}/reject",
                new RejectDraftRequest { Note = reviewNote });

            if (!response.IsSuccessStatusCode)
            {
                Snackbar.Add("Could not reject the draft.", Severity.Error);
                return;
            }

            Snackbar.Add("Draft sent back for revisions.", Severity.Success);
            Navigation.NavigateTo(PageRoutes.Drafts);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to reject draft {DraftId}", Id);
            Snackbar.Add("Could not reject the draft.", Severity.Error);
        }
        finally
        {
            isSaving = false;
        }
    }

    // PublishService queues the draft as an article and marks it published. The article
    // itself is stored by ArticleService a moment later, so it may not be on the site yet.
    private async Task PublishAsync()
    {
        isSaving = true;

        try
        {
            var response = await Http.PostAsync($"{PublishServiceBaseUrl}/api/v1/publish-draft/{Id}", null);

            if (!response.IsSuccessStatusCode)
            {
                Snackbar.Add(await ReadErrorAsync(response) ?? "Could not publish the draft.", Severity.Error);
                return;
            }

            Snackbar.Add("Draft published. The article will appear on the site in a moment.", Severity.Success);
            Navigation.NavigateTo(PageRoutes.Drafts);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to publish draft {DraftId}", Id);
            Snackbar.Add("Could not publish the draft.", Severity.Error);
        }
        finally
        {
            isSaving = false;
        }
    }

    // PublishService explains refusals: a JSON string for 409, problem details for 503.
    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return json.RootElement.ValueKind switch
            {
                JsonValueKind.String => json.RootElement.GetString(),
                JsonValueKind.Object when json.RootElement.TryGetProperty("detail", out var detail) => detail.GetString(),
                _ => null
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task ReactivateAsync()
    {
        isSaving = true;

        try
        {
            var response = await Http.PostAsync($"{DraftServiceBaseUrl}/api/drafts/{Id}/reactivate", null);

            if (!response.IsSuccessStatusCode)
            {
                Snackbar.Add("Could not reactivate the draft.", Severity.Error);
                return;
            }

            Snackbar.Add("Draft reactivated.", Severity.Success);
            Navigation.NavigateTo(PageRoutes.Drafts);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to reactivate draft {DraftId}", Id);
            Snackbar.Add("Could not reactivate the draft.", Severity.Error);
        }
        finally
        {
            isSaving = false;
        }
    }
}
