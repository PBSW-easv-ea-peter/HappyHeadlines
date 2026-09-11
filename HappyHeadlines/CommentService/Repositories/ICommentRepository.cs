using CommentService.Models;

namespace CommentService.Repositories;

public interface ICommentRepository
{
    Task<IEnumerable<CommentEntity>> GetApprovedByArticleIdAsync(string articleLocation, long articleId);
    Task<CommentEntity> CreateAsync(string articleLocation, long articleId, PostCommentRequest request, CommentStatus status);
}
