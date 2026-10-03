namespace ArticleService.Cache;

public class CacheRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CacheRefreshService> _logger;

    public CacheRefreshService(
        IServiceScopeFactory scopeFactory,
        ILogger<CacheRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // For now we only place articles from the global db.
        // "EU", "NA", "SA", "AU", "AS", "AN", "AF",
        string[] locations = ["GO"];

        while (!stoppingToken.IsCancellationRequested)
        {
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                IArticleCache cache = scope.ServiceProvider.GetRequiredService<IArticleCache>();
                await cache.RefreshCacheAsync(locations);
            }

            _logger.LogInformation("Next cache refresh in 60 minutes");

            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}