using NewsletterService.Shared.Options;

namespace NewsletterService.Setup;

public static class FeatureToggles
{
    extension(WebApplicationBuilder builder)
    {
        public void ConfigureFeatureToggles()
        {
            builder.Services
                .AddOptions<FeatureTogglesOptions>()
                .BindConfiguration("FeatureToggles");
        }
    }
}