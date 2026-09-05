using ArticleService.Models;

namespace ArticleService.Queue;

public interface IArticleQueuePublisher
{
    Task PublishArticleCreatedAsync(Article article);
}
