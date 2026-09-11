using CommentService.Models;
using CommentService.Profanity;
using CommentService.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CommentService.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController : ControllerBase
{
    private readonly ICommentRepository _repository;
    private readonly IProfanityClient _profanityClient;
    private readonly ILogger<CommentsController> _logger;

    public CommentsController(ICommentRepository repository, IProfanityClient profanityClient, ILogger<CommentsController> logger)
    {
        _repository = repository;
        _profanityClient = profanityClient;
        _logger = logger;
    }

    [HttpGet("{articleId:long}")]
    public async Task<ActionResult<IEnumerable<Comment>>> GetForArticle(long articleId)
    {
        // Only approved comments are shown to readers. A comment that could not be
        // profanity-checked (ProfanityService was down) stays hidden rather than risking
        // showing unfiltered content - see docs/comment_and_profanity_services.md.
        return Ok(await _repository.GetApprovedByArticleIdAsync(articleId));
    }

    [HttpPost("{articleId:long}")]
    public async Task<ActionResult<Comment>> Post(long articleId, PostCommentRequest request)
    {
        var status = await ClassifyAsync(request.Text);
        var comment = await _repository.CreateAsync(articleId, request, status);

        if (status == CommentStatus.Rejected)
        {
            return UnprocessableEntity("Comment was rejected: it contains profanity.");
        }

        return CreatedAtAction(nameof(GetForArticle), new { articleId }, comment);
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
