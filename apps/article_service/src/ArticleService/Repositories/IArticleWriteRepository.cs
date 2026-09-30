using ArticleService.Models;
using Messaging.Events;

namespace ArticleService.Repositories;

public interface IArticleWriteRepository
{
    Task<Article> CreateAsync(string location, UpsertArticleRequest request);

    // Returns false if an article for the same DraftId already exists (redelivered message).
    Task<bool> CreatePublishedAsync(PublishedArticleEvent published);

    Task<bool> UpdateAsync(string location, long id, UpsertArticleRequest request);
    Task<bool> DeleteAsync(string location, long id);
}
