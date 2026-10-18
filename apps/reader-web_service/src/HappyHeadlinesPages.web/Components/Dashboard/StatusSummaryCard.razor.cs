using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace HappyHeadlinesPages.web.Components.Dashboard;

public partial class StatusSummaryCard : ComponentBase
{
    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public int Count { get; set; }

    [Parameter]
    public string Icon { get; set; } = Icons.Material.Filled.Article;

    // Hex color from the logo palette (see CustomMudTheme's Logo* constants) -
    // used as a soft tinted background rather than a strong fill.
    [Parameter]
    public string AccentColor { get; set; } = "#9E9E9E";

    private string BackgroundStyle => $"background-color:{AccentColor}1F;";

    private string TextStyle => $"color:{AccentColor};";
}
