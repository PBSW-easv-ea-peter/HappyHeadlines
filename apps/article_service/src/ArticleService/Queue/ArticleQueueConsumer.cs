namespace ArticleService.Queue;

// Boilerplate seam: once ArticleQueue and PublisherService exist, this will subscribe to
// ArticleQueue and call IArticleWriteRepository.CreateAsync/UpdateAsync for each message.
// Idle for now - there is no real queue to consume yet.
public class ArticleQueueConsumer : BackgroundService
{
    private readonly ILogger<ArticleQueueConsumer> _logger;

    public ArticleQueueConsumer(ILogger<ArticleQueueConsumer> logger)
    {
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ArticleQueue consumer not wired up yet - nothing to consume.");
        return Task.CompletedTask;
    }
}
