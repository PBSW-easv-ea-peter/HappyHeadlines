namespace NewsletterService.Shared.Options;

public class FeatureTogglesOptions
{
    public List<FeatureToggle> Toggles { get; set; } = [];
}

public class FeatureToggle
{
    public required string Name { get; set; }
    public required bool State { get; set; }
}