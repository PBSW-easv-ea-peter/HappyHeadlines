using HappyHeadlinesPages.web.Layout;
using HappyHeadlinesPages.web.Models;

namespace HappyHeadlinesPages.web.Components.Dashboard;

// Single source of truth for status -> logo-color mapping, shared between
// DraftStatusBadge and the small left-border accents on list items so they
// never drift apart.
public static class DraftAccentColors
{
    public static string For(DraftStatus status, string? reviewNote) => status switch
    {
        DraftStatus.WorkInProgress when !string.IsNullOrWhiteSpace(reviewNote) => CustomMudTheme.LogoPink,
        DraftStatus.WorkInProgress => CustomMudTheme.LogoYellow,
        DraftStatus.PendingApproval => CustomMudTheme.LogoPurple,
        DraftStatus.Approved => CustomMudTheme.LogoGreen,
        DraftStatus.Published => CustomMudTheme.LogoBlue,
        _ => "#9E9E9E"
    };
}
