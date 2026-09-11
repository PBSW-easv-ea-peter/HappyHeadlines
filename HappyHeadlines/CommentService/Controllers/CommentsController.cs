using CommentService.Handlers;
using CommentService.Models;
using Microsoft.AspNetCore.Mvc;

namespace CommentService.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController : ControllerBase
{
    private static readonly HashSet<string> ValidLocations =
        new(StringComparer.OrdinalIgnoreCase) { "EU", "NA", "SA", "AU", "AS", "AN", "AF", "GO" };

    private readonly ICommentHandler _handler;

    public CommentsController(ICommentHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("{location}/{articleId:long}")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetForArticle(string location, long articleId)
    {
        if (!ValidLocations.Contains(location))
        {
            return BadRequest($"Unknown location '{location}'.");
        }

        // Only approved comments are shown to readers. A comment that could not be
        // profanity-checked (ProfanityService was down) stays hidden rather than risking
        // showing unfiltered content - see docs/comment_and_profanity_requirements.md.
        return Ok(await _handler.GetApprovedAsync(location, articleId));
    }

    [HttpPost("{location}/{articleId:long}")]
    public async Task<ActionResult<CommentDto>> Post(string location, long articleId, PostCommentRequest request)
    {
        if (!ValidLocations.Contains(location))
        {
            return BadRequest($"Unknown location '{location}'.");
        }

        if (string.IsNullOrWhiteSpace(request.AuthorName) || string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("AuthorName and Text must not be empty.");
        }

        var (comment, rejected) = await _handler.PostAsync(location, articleId, request);

        if (rejected)
        {
            return UnprocessableEntity("Comment was rejected: it contains profanity.");
        }

        return CreatedAtAction(nameof(GetForArticle), new { location, articleId }, comment);
    }
}
