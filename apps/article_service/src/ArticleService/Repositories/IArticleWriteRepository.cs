using ArticleService.Models;
using Messaging.Events;

namespace ArticleService.Repositories;

public interface IArticleWriteRepository
{
    Task<Article> CreateAsync(string location, UpsertArticleRequest request);

    // Returns the new article's id, or null if an article for the same DraftId already exists
    // (redelivered message).
    Task<Guid?> CreatePublishedAsync(PublishedArticleEvent published);

    Task<bool> UpdateAsync(string location, Guid id, UpsertArticleRequest request);
    Task<bool> DeleteAsync(string location, Guid id);
}
