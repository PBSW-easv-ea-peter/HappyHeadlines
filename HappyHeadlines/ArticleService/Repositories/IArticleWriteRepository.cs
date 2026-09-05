using ArticleService.Models;

namespace ArticleService.Repositories;

public interface IArticleWriteRepository
{
    Task<Article> CreateAsync(string location, UpsertArticleRequest request);
    Task<bool> UpdateAsync(string location, long id, UpsertArticleRequest request);
    Task<bool> DeleteAsync(string location, long id);
}
