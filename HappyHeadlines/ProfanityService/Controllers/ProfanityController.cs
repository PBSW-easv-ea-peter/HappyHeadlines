using Microsoft.AspNetCore.Mvc;
using ProfanityService.Models;
using ProfanityService.Repositories;

namespace ProfanityService.Controllers;

[ApiController]
[Route("api/profanity")]
public class ProfanityController : ControllerBase
{
    private readonly IProfanityRepository _repository;

    public ProfanityController(IProfanityRepository repository)
    {
        _repository = repository;
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

        return Ok(await _repository.IsProfaneAsync(request.Word));
    }
}
