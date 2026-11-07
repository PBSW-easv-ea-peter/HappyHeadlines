using NewsletterService.NewsletterManager;
using NewsletterService.Shared;
using NewsletterService.Shared.Options;

namespace NewsletterService.Setup;

public static class StartIntervals
{
    public static void AddNewsletterServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        
        // Configure NewsletterOptions
        services.Configure<NewsletterOptions>(configuration.GetSection("NewsletterIntervals"));
        
        var intervalsConfig = configuration.GetSection("NewsletterIntervals:Intervals").Get<List<IntervalOptions>>();
        if (intervalsConfig == null || intervalsConfig.Count == 0)
        {
            throw new InvalidOperationException("No newsletter intervals configured in NewsletterIntervals:Intervals");
        }

        // Validate all intervals
        foreach (var interval in intervalsConfig)
        {
            if (interval.HourOfDay is > 23 or < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(interval.HourOfDay),
                    "HourOfDay needs to be between 0 and 23");

            if (interval.Interval == NewsletterIntervals.Weekly && !interval.DayOfWeek.HasValue)
            {
                throw new InvalidOperationException("Weekly intervals require DayOfWeek to be set");
            }
        }
        
        // Register a single background service that checks all intervals every hour
        services.AddSingleton<IHostedService, NewsletterBackgroundService>();
    }
}
