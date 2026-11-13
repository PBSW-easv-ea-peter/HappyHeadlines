using ArticleService.Models;

namespace ArticleService.Cache;

public interface IArticleCache
{
    Task<IList<Article>> GetArticlesAsync(string location);

    // The composed front page, exactly as returned to the client. null is a miss.
    Task<IList<Article>?> GetFrontPageAsync(string location, FrontPageQuery query);
    Task StoreFrontPageAsync(string location, FrontPageQuery query, IList<Article> articles);
    Task<Article?> GetArticleByIdAsync(string location, Guid id);
    Task RefreshCacheAsync(params string[] locations);

    // Re-reads one article from the database and updates its key and its place on the front
    // page. Removes it from the cache if it's deleted, unpublished or older than the window.
    Task RefreshArticleAsync(string location, Guid id);
    Task RemoveArticleAsync(string location, Guid id);
}
