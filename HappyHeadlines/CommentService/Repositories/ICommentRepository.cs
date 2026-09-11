using CommentService.Models;

namespace CommentService.Repositories;

public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetApprovedByArticleIdAsync(long articleId);
    Task<Comment> CreateAsync(long articleId, PostCommentRequest request, CommentStatus status);
}
