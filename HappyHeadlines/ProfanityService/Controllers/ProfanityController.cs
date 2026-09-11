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

    // POST, not GET or PUT: this checks a word against the profanity list - a computation
    // that returns a result without creating or replacing any resource. POST also avoids
    // the URL-encoding issues GET query parameters would bring if this is later extended
    // from a single word to a full sentence/comment. See docs/comment_and_profanity_services.md.
    [HttpPost("check")]
    public async Task<ActionResult<bool>> Check(ProfanityCheckRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Word))
        {
            return BadRequest("Word must not be empty.");
        }

        return Ok(await _checker.CheckAsync(request.Word));
    }
}
