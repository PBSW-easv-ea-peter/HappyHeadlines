using System.Globalization;
using System.Reflection;

namespace HappyHeadlinesPages.web;

// Reads the BuildTimeUtc stamp the csproj writes into this assembly on every
// build. Shown in the reader footer and the journalist nav drawer: if it is
// older than the last deploy, the browser is running a cached build.
public static class BuildInfo
{
    public static readonly DateTimeOffset? BuiltAt = ReadBuildTime();

    public static string Display =>
        BuiltAt?.ToLocalTime().ToString("d MMM yyyy HH:mm") ?? "unknown";

    private static DateTimeOffset? ReadBuildTime()
    {
        var value = typeof(BuildInfo).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "BuildTimeUtc")?.Value;

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var builtAt)
            ? builtAt
            : null;
    }
}
