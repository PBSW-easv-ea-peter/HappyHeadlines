using CommentService.Models;

namespace CommentService.Handlers;

public interface ICommentHandler
{
    Task<IEnumerable<CommentDto>> GetApprovedAsync(string articleLocation, long articleId);
    Task<(CommentDto Comment, bool Rejected)> PostAsync(string articleLocation, long articleId, PostCommentRequest request);
}
