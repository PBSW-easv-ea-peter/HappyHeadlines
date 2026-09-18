using MudBlazor;

namespace HappyHeadlinesApp.Layout;

public static class CustomMudTheme
{
    public static MudTheme Theme => new()
    {
        PaletteLight = LightPalette,
        PaletteDark = DarkPalette,
        LayoutProperties = new LayoutProperties(),

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Jost", "sans-serif", "Helvetica", "Arial"]
            },
            Body2 = new Body1Typography
            {
                FontFamily = ["Jost", "sans-serif"],
                FontSize = "1rem",
                FontWeight = "300"
            },
            H1 = new H1Typography
            {
                FontFamily = ["EB Garamond", "sans-serif"],
                FontSize = "3.75rem"
            },
            H3 = new H3Typography
            {
                FontFamily = ["EB Garamond", "sans-serif"],
            },
            H6 = new H6Typography
            {
                FontSize = "1.1rem",
                FontWeight = "500",
            }
        },
    };

    private static readonly PaletteLight LightPalette = new()
    {
        Primary = "#f4e2d3",
        Secondary = "#bbddfa",

        AppbarText = "#424242",
        AppbarBackground = "rgba(255,255,255,1)",
        DrawerBackground = "#ffffff",
        GrayLight = "#e8e8e8",
        GrayLighter = "#f9f9f9",
    };

    private static readonly PaletteDark DarkPalette = new()
    {
        Primary = "#7e6fff",
        Surface = "#1e1e2d",
        Background = "#1a1a27",
        BackgroundGray = "#151521",
        AppbarText = "#92929f",
        AppbarBackground = "rgba(26,26,39,1)",
        DrawerBackground = "#1a1a27",
        ActionDefault = "#74718e",
        ActionDisabled = "#9999994d",
        ActionDisabledBackground = "#605f6d4d",
        TextPrimary = "#b2b0bf",
        TextSecondary = "#92929f",
        TextDisabled = "#ffffff33",
        DrawerIcon = "#92929f",
        DrawerText = "#92929f",
        GrayLight = "#2a2833",
        GrayLighter = "#1e1e2d",
        Info = "#4a86ff",
        Success = "#3dcb6c",
        Warning = "#ffb545",
        Error = "#ff3f5f",
        LinesDefault = "#33323e",
        TableLines = "#33323e",
        Divider = "#292838",
        OverlayLight = "#1e1e2d80",
    };
}
