namespace NewsletterService.Intervals;

public class NewsletterBackgroundService<T> : BackgroundService where T : class, INewsletterService
{
    private readonly ILogger<NewsletterBackgroundService<T>> _logger;
    private readonly IServiceProvider _serviceProvider;
    
    public NewsletterBackgroundService(
        ILogger<NewsletterBackgroundService<T>> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Create a scope to resolve the scoped INewsletterService
            using (var scope = _serviceProvider.CreateScope())
            {
                var newsletterService = scope.ServiceProvider.GetRequiredService<T>();
                
                await newsletterService.Delay(stoppingToken);

                try
                {
                    await newsletterService.ExecuteTask(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing {Interval} newsletter task", newsletterService.Interval);
                }
            }
        }
    }
}