using CommentService.Models;

namespace CommentService.Handlers;

public interface ICommentHandler
{
    Task<IEnumerable<CommentDto>> GetApprovedAsync(string articleLocation, long articleId);
    Task<(CommentDto Comment, CommentStatus Status)> PostAsync(string articleLocation, long articleId, PostCommentRequest request, CancellationToken cancellationToken = default);
}
