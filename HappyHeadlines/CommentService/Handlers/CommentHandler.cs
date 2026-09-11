using CommentService.Models;
using CommentService.Profanity;
using CommentService.Repositories;

namespace CommentService.Handlers;

// Orchestrates comment classification and persistence, kept out of the controller so
// CommentsController only deals with HTTP concerns (route binding, status codes).
public class CommentHandler : ICommentHandler
{
    private readonly ICommentRepository _repository;
    private readonly IProfanityClient _profanityClient;
    private readonly ILogger<CommentHandler> _logger;

    public CommentHandler(ICommentRepository repository, IProfanityClient profanityClient, ILogger<CommentHandler> logger)
    {
        _repository = repository;
        _profanityClient = profanityClient;
        _logger = logger;
    }

    public async Task<IEnumerable<CommentDto>> GetApprovedAsync(string articleLocation, long articleId)
    {
        var entities = await _repository.GetApprovedByArticleIdAsync(articleLocation, articleId);
        return entities.Select(CommentDto.FromEntity);
    }

    public async Task<(CommentDto Comment, bool Rejected)> PostAsync(string articleLocation, long articleId, PostCommentRequest request)
    {
        var status = await ClassifyAsync(request.Text);
        var entity = await _repository.CreateAsync(articleLocation, articleId, request, status);
        return (CommentDto.FromEntity(entity), status == CommentStatus.Rejected);
    }

    private async Task<CommentStatus> ClassifyAsync(string text)
    {
        var words = text
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var word in words)
        {
            var result = await _profanityClient.CheckAsync(word);

            if (result.CircuitOpen)
            {
                // Fault isolation in practice: ProfanityService is unavailable and the
                // circuit breaker has tripped. CommentService itself stays up and keeps
                // accepting comments (design to be disabled + isolate faults) instead of
                // failing the whole request - it just cannot vouch for this one yet.
                _logger.LogWarning("ProfanityService unavailable - comment queued for review instead of being rejected outright.");
                return CommentStatus.PendingProfanityCheck;
            }

            if (result.IsProfane)
            {
                return CommentStatus.Rejected;
            }
        }

        return CommentStatus.Approved;
    }
}
