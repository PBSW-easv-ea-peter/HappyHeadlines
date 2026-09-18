using ArticleService.Models;
using ArticleService.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace ArticleService.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticlesController : ControllerBase
{
    private static readonly HashSet<string> ValidLocations =
        new(StringComparer.OrdinalIgnoreCase) { "EU", "NA", "SA", "AU", "AS", "AN", "AF", "GO" };

    private readonly IArticleReadRepository _readRepository;
    private readonly IArticleWriteRepository _writeRepository;

    public ArticlesController(IArticleReadRepository readRepository, IArticleWriteRepository writeRepository)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
    }

    [HttpGet("{location}")]
    public async Task<ActionResult<IEnumerable<Article>>> GetAll(string location)
    {
        if (!ValidLocations.Contains(location))
        {
            return BadRequest($"Unknown location '{location}'.");
        }

        return Ok(await _readRepository.GetAllAsync(location));
    }

    [HttpGet("{location}/{id:long}")]
    public async Task<ActionResult<Article>> GetById(string location, long id)
    {
        if (!ValidLocations.Contains(location))
        {
            return BadRequest($"Unknown location '{location}'.");
        }

        var article = await _readRepository.GetByIdAsync(location, id);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpPost("{location}")]
    public async Task<ActionResult<Article>> Create(string location, UpsertArticleRequest request)
    {
        if (!ValidLocations.Contains(location))
        {
            return BadRequest($"Unknown location '{location}'.");
        }

        var article = await _writeRepository.CreateAsync(location, request);
        return CreatedAtAction(nameof(GetById), new { location, id = article.Id }, article);
    }

    [HttpPut("{location}/{id:long}")]
    public async Task<IActionResult> Update(string location, long id, UpsertArticleRequest request)
    {
        if (!ValidLocations.Contains(location))
        {
            return BadRequest($"Unknown location '{location}'.");
        }

        var updated = await _writeRepository.UpdateAsync(location, id, request);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{location}/{id:long}")]
    public async Task<IActionResult> Delete(string location, long id)
    {
        if (!ValidLocations.Contains(location))
        {
            return BadRequest($"Unknown location '{location}'.");
        }

        var deleted = await _writeRepository.DeleteAsync(location, id);
        return deleted ? NoContent() : NotFound();
    }
}
