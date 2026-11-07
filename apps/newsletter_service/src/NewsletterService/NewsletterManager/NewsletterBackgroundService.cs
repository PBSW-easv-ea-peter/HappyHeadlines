using Microsoft.Extensions.Options;
using NewsletterService.NewsletterDistributors.EmailService;
using NewsletterService.Services.ArticleService;
using NewsletterService.Services.SubscriberService;
using NewsletterService.Shared.Options;

namespace NewsletterService.NewsletterManager;

/// <summary>
/// Single background service that checks all configured intervals every hour
/// and executes any that are due to run
/// </summary>
public class NewsletterBackgroundService : BackgroundService
{
    private readonly ILogger<NewsletterBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly NewsletterOptions _newsletterOptions;
    private List<IntervalOptions> Intervals =>
        _newsletterOptions.Intervals ?? [];
    private readonly TimeProvider _timeProvider;

    public NewsletterBackgroundService(
        ILogger<NewsletterBackgroundService> logger,
        IServiceProvider serviceProvider,
        IOptions<NewsletterOptions> options,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _timeProvider = timeProvider;
        _newsletterOptions = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Newsletter background service started. Checking {Count} intervals every hour", Intervals.Count);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndExecuteIntervals(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in newsletter background service");
            }

            // Wait for the next hour
            await WaitUntilNextHour(stoppingToken);
        }

        _logger.LogInformation("Newsletter background service stopped");
    }

    private async Task CheckAndExecuteIntervals(CancellationToken stoppingToken)
    {
        DateTime now = _timeProvider.GetLocalNow().DateTime;
        _logger.LogDebug("Checking intervals at {Time}", now);

        foreach (var interval in Intervals)
        {
            try
            {
                // Create a scope for each execution
                using var scope = _serviceProvider.CreateScope();
                var executor = CreateExecutor(scope, interval);
                    
                if (executor.ShouldExecuteNow())
                {
                    _logger.LogInformation(
                        "Executing {Interval} newsletter (Hour: {Hour}, Day: {Day})",
                        interval.Interval,
                        interval.HourOfDay,
                        interval.DayOfWeek);
                        
                    await executor.ExecuteAsync(stoppingToken);
                }
                else
                {
                    _logger.LogDebug(
                        "Skipping {Interval} newsletter - not due yet",
                        interval.Interval);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error executing {Interval} newsletter task",
                    interval.Interval);
            }
        }
    }

    private NewsletterExecutor CreateExecutor(IServiceScope scope, IntervalOptions interval)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<NewsletterExecutor>>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        
        var subscriberService = scope.ServiceProvider.GetRequiredService<ISubscriberService>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var emailFactory = scope.ServiceProvider.GetRequiredService<IEmailFactory>();
        
        var articleService = scope.ServiceProvider.GetRequiredService<IArticleService>();

        return new NewsletterExecutor(
            interval,
            logger,
            timeProvider,
            subscriberService,
            emailService,
            emailFactory,
            articleService);
    }

    private async Task WaitUntilNextHour(CancellationToken stoppingToken)
    {
        DateTime now = _timeProvider.GetLocalNow().DateTime;
        
        // Calculate time until next hour
        DateTime nextHour = now.Date.AddHours(now.Hour + 1);
        TimeSpan delay = nextHour - now;
        
        _logger.LogDebug("Waiting {Delay} until next hour check", delay);
        
        await Task.Delay(delay, _timeProvider, stoppingToken);
    }
}
