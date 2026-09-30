using System.Web;
using HappyHeadlinesPages.web.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace HappyHeadlines.reader.wasm.Layout;

// The selected edition lives in the ?region= query string (not in layout state)
// so pages read it with [SupplyParameterFromQuery] and links stay shareable.
public partial class ReaderLayout : IDisposable
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private bool _isDarkMode;
    private MudTheme? _theme;
    private string _region = ReaderRegions.Default;

    private static string Today => DateTime.Now.ToString("dddd, d MMMM yyyy");

    private string HomeHref => _region == ReaderRegions.Default ? "./" : $"./?region={_region}";

    protected override void OnInitialized()
    {
        _theme = CustomMudTheme.Theme;
        _region = RegionFromUri(Navigation.Uri);

        Navigation.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _region = RegionFromUri(e.Location);
        StateHasChanged();
    }

    private void OnRegionChanged(string region)
    {
        // Keep the reader on the current page/section, just switch edition.
        Navigation.NavigateTo(Navigation.GetUriWithQueryParameter("region", region));
    }

    private static string RegionFromUri(string uri)
    {
        var query = new Uri(uri).Query;

        return ReaderRegions.Normalize(HttpUtility.ParseQueryString(query)["region"]);
    }

    private void DarkModeToggle()
    {
        _isDarkMode = !_isDarkMode;
    }

    private string DarkLightModeButtonIcon => _isDarkMode switch
    {
        true => Icons.Material.Rounded.LightMode,
        false => Icons.Material.Outlined.DarkMode,
    };

    public void Dispose()
    {
        Navigation.LocationChanged -= OnLocationChanged;
    }
}
