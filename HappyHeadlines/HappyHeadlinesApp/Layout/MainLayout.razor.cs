using MudBlazor;

namespace HappyHeadlinesApp.Layout;

public partial class MainLayout
{
    private bool _drawerOpen = true;
    private bool _isDarkMode = false;
    private MudTheme? _theme;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        _theme = new MudTheme(); // CustomMbTheme.Theme;
    }

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
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
}
