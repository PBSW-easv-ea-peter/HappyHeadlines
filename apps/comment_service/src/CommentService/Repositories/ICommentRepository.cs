using CommentService.Models;

namespace CommentService.Repositories;

public interface ICommentRepository
{
    Task<IEnumerable<CommentEntity>> GetApprovedByArticleIdAsync(string articleLocation, Guid articleId);
    Task<CommentEntity> CreateAsync(string articleLocation, Guid articleId, PostCommentRequest request, CommentStatus status);
}
