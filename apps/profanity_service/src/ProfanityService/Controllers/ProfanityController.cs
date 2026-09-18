using Microsoft.AspNetCore.Mvc;
using ProfanityService.Checking;
using ProfanityService.Models;

namespace ProfanityService.Controllers;

[ApiController]
[Route("api/profanity")]
public class ProfanityController : ControllerBase
{
    private readonly IProfanityChecker _checker;

    public ProfanityController(IProfanityChecker checker)
    {
        _checker = checker;
    }

    // POST, not GET or PUT: this checks text against the profanity list - a computation
    // that returns a result without creating or replacing any resource. POST also avoids
    // the URL-encoding issues GET query parameters would bring for a full sentence/comment.
    // See docs/comment_and_profanity_services.md.
    [HttpPost("check")]
    public async Task<ActionResult<IReadOnlyList<string>>> Check(ProfanityCheckRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest("Text must not be empty.");
        }

        return Ok(await _checker.CheckAsync(request.Text));
    }
}
