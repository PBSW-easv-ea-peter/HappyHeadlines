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

    // Exactly what a front page shows, composed in one query and cached as a whole (GAF: Defer
    // Fetching - fewer rows). The client passes its own layout: the reader front page asks for
    // top = 1 hero + 5 popular and perSection = 3 cards.
    [HttpGet("{location}/frontpage")]
    public async Task<ActionResult<IEnumerable<Article>>> GetFrontPage(
        string location,
        [FromQuery] int top = 10,
        [FromQuery] int perSection = 0,
        [FromQuery] string? section = null)
    {
        if (!ValidLocations.Contains(location))
        {
            return BadRequest($"Unknown location '{location}'.");
        }

        var query = new FrontPageQuery(top, perSection, section);
        if (query.Validate() is { } error)
        {
            return BadRequest(error);
        }

        IList<Article>? cached = await _cache.GetFrontPageAsync(location, query);
        if (cached is not null)
        {
            return Ok(cached);
        }

        IList<Article> articles = [.. await _readRepository.GetFrontPageAsync(location, query)];
        await _cache.StoreFrontPageAsync(location, query, articles);
        return Ok(articles);
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
