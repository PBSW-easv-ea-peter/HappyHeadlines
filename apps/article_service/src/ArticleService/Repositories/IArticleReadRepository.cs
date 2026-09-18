using ArticleService.Models;

namespace ArticleService.Repositories;

public interface IArticleReadRepository
{
    Task<IEnumerable<Article>> GetAllAsync(string location);
    Task<Article?> GetByIdAsync(string location, long id);
}
