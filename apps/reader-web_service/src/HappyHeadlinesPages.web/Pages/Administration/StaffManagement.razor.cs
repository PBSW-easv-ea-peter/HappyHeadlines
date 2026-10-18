using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HappyHeadlinesPages.web.Pages.Administration;

// Visual mockup only - no journalist is actually created/edited/removed here.
public partial class StaffManagement : ComponentBase
{
    protected override async Task OnInitializedAsync()
    {
        await JournalistState.EnsureLoadedAsync();
    }

    private void ShowComingSoon() =>
        Snackbar.Add("Staff management isn't wired up yet - this page is a placeholder.", Severity.Info);

    private static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p => p[0])).ToUpperInvariant();
    }
}
