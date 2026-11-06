using Microsoft.Extensions.Options;
using NewsletterService.Intervals;
using NewsletterService.Shared.Options;

namespace NewsletterService.Setup;

public static class StartIntervals
{
    public static void AddNewsletterServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        
        var intervalsConfig = configuration.GetSection("NewsletterIntervals:Intervals").Get<List<IntervalOptions>>();
        if (intervalsConfig == null || intervalsConfig.Count == 0)
        {
            throw new InvalidOperationException("No newsletter intervals configured in NewsletterIntervals:Intervals");
        }

        // Register each interval service and its corresponding background service
        foreach (var interval in intervalsConfig)
        {
            if (interval.HourOfDay is > 23 or < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(interval.HourOfDay),
                    "HourOfDay needs to be between 0 and 23");

            if (interval.IntervalName.Equals("Daily", StringComparison.OrdinalIgnoreCase))
            {
                services.AddScoped<DailyNewsletterService>(provider =>
                    new DailyNewsletterService(
                        Options.Create(interval),
                        provider.GetRequiredService<ILogger<DailyNewsletterService>>(),
                        provider.GetRequiredService<TimeProvider>()));

                services.AddSingleton<IHostedService>(provider =>
                    new NewsletterBackgroundService<DailyNewsletterService>(
                        provider.GetRequiredService<ILogger<NewsletterBackgroundService<DailyNewsletterService>>>(),
                        provider));
            }
            else if (interval.IntervalName.Equals("Weekly", StringComparison.OrdinalIgnoreCase))
            {
                if (!interval.DayOfWeek.HasValue ||
                    !Enum.IsDefined(typeof(DayOfWeek), interval.DayOfWeek.Value))
                {
                    throw new InvalidOperationException("Invalid DayOfWeek value");
                }

                services.AddScoped<WeeklyNewsletterService>(provider =>
                    new WeeklyNewsletterService(
                        Options.Create(interval),
                        provider.GetRequiredService<ILogger<WeeklyNewsletterService>>(),
                        provider.GetRequiredService<TimeProvider>()));

                services.AddSingleton<IHostedService>(provider =>
                    new NewsletterBackgroundService<WeeklyNewsletterService>(
                        provider.GetRequiredService<ILogger<NewsletterBackgroundService<WeeklyNewsletterService>>>(),
                        provider));
            }
        }
    }
}
