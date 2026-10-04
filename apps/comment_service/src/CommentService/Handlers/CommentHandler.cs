using CommentService.Cache;
using CommentService.Models;
using CommentService.Profanity;
using CommentService.Repositories;

namespace CommentService.Handlers;

// Orchestrates comment classification, caching and persistence, kept out of the controller
// so CommentsController only deals with HTTP concerns (route binding, status codes).
public class CommentHandler : ICommentHandler
{
    private readonly ICommentRepository _repository;
    private readonly IProfanityClient _profanityClient;
    private readonly ICommentCache _cache;
    private readonly ILogger<CommentHandler> _logger;

    public CommentHandler(ICommentRepository repository, IProfanityClient profanityClient, ICommentCache cache, ILogger<CommentHandler> logger)
    {
        _repository = repository;
        _profanityClient = profanityClient;
        _cache = cache;
        _logger = logger;
    }

    // Cache miss approach: an article's comments are only cached once they're read and
    // weren't cached yet (docs/Caching.md).
    public async Task<IEnumerable<CommentDto>> GetApprovedAsync(string articleLocation, Guid articleId)
    {
        var cached = await _cache.GetAsync(articleLocation, articleId);
        if (cached is not null)
        {
            return cached;
        }

        var entities = await _repository.GetApprovedByArticleIdAsync(articleLocation, articleId);
        var comments = entities.Select(CommentDto.FromEntity).ToList();

        await _cache.SetAsync(articleLocation, articleId, comments);
        return comments;
    }

    public async Task<(CommentDto Comment, CommentStatus Status)> PostAsync(string articleLocation, Guid articleId, PostCommentRequest request, CancellationToken ct = default)
    {
        var status = await ClassifyAsync(request.Text, ct);
        var entity = await _repository.CreateAsync(articleLocation, articleId, request, status);
        _logger.LogInformation(
            "Comment {CommentId} on article {ArticleId} created with status {Status}",
            entity.Id, articleId, status);

        var comment = CommentDto.FromEntity(entity);

        // Write-through: only approved comments are cached, since only those are shown.
        if (status == CommentStatus.Approved)
        {
            await _cache.AppendIfCachedAsync(articleLocation, articleId, comment);
        }

        return (comment, status);
    }

    private async Task<CommentStatus> ClassifyAsync(string text, CancellationToken cancellationToken)
    {
        var result = await _profanityClient.CheckAsync(text, cancellationToken);

        if (result.Unavailable)
        {
            // Fault isolation in practice: ProfanityService gave no answer (circuit open,
            // unreachable, timed out or erroring). CommentService itself stays up and keeps
            // accepting comments (design to be disabled + isolate faults) instead of
            // failing the whole request - it just cannot vouch for this one yet.
            _logger.LogWarning("ProfanityService unavailable - comment queued for review instead of being rejected outright.");
            return CommentStatus.PendingProfanityCheck;
        }

        return result.IsProfane ? CommentStatus.Rejected : CommentStatus.Approved;
    }
}
