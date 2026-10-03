using System.Reflection;

namespace ArticleService.Sharding;

using System.ComponentModel;

public enum ArticleLocation
{
    [Description("Europe")]
    EU,

    [Description("North America")]
    NA,

    [Description("South America")]
    SA,

    [Description("Australia")]
    AU,

    [Description("Asia")]
    AS,

    [Description("Antarctica")]
    AN,

    [Description("Africa")]
    AF,

    [Description("Global")]
    GO,
}

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }
}