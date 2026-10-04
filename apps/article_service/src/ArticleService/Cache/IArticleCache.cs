using ArticleService.Models;

namespace ArticleService.Cache;

public interface IArticleCache
{
    Task<IList<Article>> GetArticlesAsync(string location);
    Task<Article?> GetArticleByIdAsync(string location, Guid id);
    Task RefreshCacheAsync(params string[] locations);
    Task InvalidateArticleAsync(string location, Guid id);
}