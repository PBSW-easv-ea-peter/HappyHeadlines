using CommentService.Models;

namespace CommentService.Cache;

// Approved comments for the most recently accessed articles. A failing cache never fails
// the request - callers fall back to the database (see docs/Caching.md).
public interface ICommentCache
{
    // Null means a miss: the article isn't cached (or the cache is unavailable).
    Task<IReadOnlyList<CommentDto>?> GetAsync(string articleLocation, Guid articleId);

    // Fills the cache after a miss. May evict the least recently used article.
    Task SetAsync(string articleLocation, Guid articleId, IReadOnlyList<CommentDto> comments);

    // Write-through for a newly approved comment. Does nothing if the article isn't cached.
    Task AppendIfCachedAsync(string articleLocation, Guid articleId, CommentDto comment);
}
