using ArticleService.Models;

namespace ArticleService.Queue;

// Boilerplate seam for wiring up the real ArticleQueue next week.
public class NoOpArticleQueuePublisher : IArticleQueuePublisher
{
    private readonly ILogger<NoOpArticleQueuePublisher> _logger;

    public NoOpArticleQueuePublisher(ILogger<NoOpArticleQueuePublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishArticleCreatedAsync(Article article)
    {
        _logger.LogInformation(
            "ArticleQueue not wired up yet - would publish article {ArticleId}", article.Id);
        return Task.CompletedTask;
    }
}
