using ArticleService.Cache;
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
    private readonly IArticleCache _cache;

    public ArticlesController(
        IArticleReadRepository readRepository,
        IArticleWriteRepository writeRepository,
        IArticleCache cache)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _cache = cache;
    }

    [HttpGet("{location}")]
    public async Task<ActionResult<IEnumerable<Article>>> GetAll(string location)
    {
        if (!ValidLocations.Contains(location))
        {
            return BadRequest($"Unknown location '{location}'.");
        }

        IList<Article> cachedArticles = await _cache.GetArticlesAsync(location);
        if (cachedArticles.Any())
        {
            return Ok(cachedArticles);
        }

        return Ok(await _readRepository.GetAllAsync(location));
    }

    [HttpGet("{location}/{id:guid}")]
    public async Task<ActionResult<Article>> GetById(string location, Guid id)
    {
        if (!ValidLocations.Contains(location))
        {
            return BadRequest($"Unknown location '{location}'.");
        }

        Article? cachedArticle = await _cache.GetArticleByIdAsync(location, id);
        if (cachedArticle != null)
        {
            return Ok(cachedArticle);
        }

        Article? article = await _readRepository.GetByIdAsync(location, id);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpPost("{location}")]
    public async Task<ActionResult<Article>> Create(string location, UpsertArticleRequest request)
    {
        if (!ValidLocations.Contains(location))
        {
            return BadRequest($"Unknown location '{location}'.");
        }

        Article article = await _writeRepository.CreateAsync(location, request);
        await _cache.RefreshArticleAsync(location, article.Id);
        return CreatedAtAction(nameof(GetById), new { location, id = article.Id }, article);
    }

    [HttpPut("{location}/{id:guid}")]
    public async Task<IActionResult> Update(string location, Guid id, UpsertArticleRequest request)
    {
        if (!ValidLocations.Contains(location))
        {
            return BadRequest($"Unknown location '{location}'.");
        }

        bool updated = await _writeRepository.UpdateAsync(location, id, request);
        if (updated)
        {
            await _cache.RefreshArticleAsync(location, id);
        }
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{location}/{id:guid}")]
    public async Task<IActionResult> Delete(string location, Guid id)
    {
        if (!ValidLocations.Contains(location))
        {
            return BadRequest($"Unknown location '{location}'.");
        }

        bool deleted = await _writeRepository.DeleteAsync(location, id);
        if (deleted)
        {
            await _cache.RemoveArticleAsync(location, id);
        }
        return deleted ? NoContent() : NotFound();
    }
}
