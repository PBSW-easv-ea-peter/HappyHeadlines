using CommentService.Models;

namespace CommentService.Handlers;

public interface ICommentHandler
{
    Task<IEnumerable<CommentDto>> GetApprovedAsync(string articleLocation, Guid articleId);
    Task<(CommentDto Comment, CommentStatus Status)> PostAsync(string articleLocation, Guid articleId, PostCommentRequest request, CancellationToken cancellationToken = default);
}
