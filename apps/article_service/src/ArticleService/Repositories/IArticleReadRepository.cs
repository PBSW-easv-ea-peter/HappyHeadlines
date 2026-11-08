using ArticleService.Models;

namespace ArticleService.Repositories;

public interface IArticleReadRepository
{
    Task<IEnumerable<Article>> GetAllAsync(string location);
    Task<IEnumerable<Article>> GetPublishedSinceAsync(string location, DateTimeOffset since);
    Task<Article?> GetByIdAsync(string location, Guid id);
}
