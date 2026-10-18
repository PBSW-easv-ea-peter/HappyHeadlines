using HappyHeadlinesPages.web.Layout;
using HappyHeadlinesPages.web.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HappyHeadlinesPages.web.Components.Dashboard;

public partial class DraftStatusBadge : ComponentBase
{
    [Parameter]
    public DraftStatus Status { get; set; }

    [Parameter]
    public string[] FlaggedWords { get; set; } = [];

    // Set when this draft was just sent back by a reviewer - distinguishes
    // "Needs changes" from a plain fresh "Draft", both of which are WorkInProgress.
    [Parameter]
    public string? ReviewNote { get; set; }

    private bool HasFlaggedWords => FlaggedWords.Length > 0;

    private bool NeedsChanges => Status == DraftStatus.WorkInProgress && !string.IsNullOrWhiteSpace(ReviewNote);

    private string Label => Status switch
    {
        DraftStatus.WorkInProgress => NeedsChanges ? "Needs changes" : "Draft",
        DraftStatus.PendingApproval => "Awaiting review",
        DraftStatus.Approved => "Approved",
        DraftStatus.Published => "Published",
        DraftStatus.Archived => "Archived",
        _ => Status.ToString()
    };

    // MudBlazor's Color enum doesn't cover the logo's purple/pink, so those two
    // states render via an inline style instead; everything else uses the enum
    // (and picks up light/dark theme colors automatically).
    private string? AccentStyle => Status switch
    {
        DraftStatus.PendingApproval => $"background-color:{CustomMudTheme.LogoPurple};color:white;",
        DraftStatus.WorkInProgress when NeedsChanges => $"background-color:{CustomMudTheme.LogoPink};color:white;",
        _ => null
    };

    private Color ChipColor => Status switch
    {
        DraftStatus.WorkInProgress => Color.Warning,
        DraftStatus.Approved => Color.Success,
        DraftStatus.Published => Color.Info,
        DraftStatus.Archived => Color.Default,
        _ => Color.Default
    };

    private string TooltipText =>
        $"Flagged during a previous profanity check: {string.Join(", ", FlaggedWords)}";
}
