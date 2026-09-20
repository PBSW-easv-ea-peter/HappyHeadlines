using HappyHeadlinesPages.web.Models;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace HappyHeadlinesPages.web.Pages.Drafts;

public partial class EditDraft : ComponentBase
{
    private const string DraftServiceBaseUrl = "http://localhost:8083";

    [Parameter]
    public long Id { get; set; }

    private Draft? _draft;
    private bool isLoading = true;
    private bool isSaving;

    private string title = string.Empty;
    private string breadtext = string.Empty;
    private string location = string.Empty;
    private long sectionId;
    private string reviewNote = string.Empty;

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
                    SectionId = sectionId
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
