using ArticleService.Cache;
using ArticleService.Models;
using ArticleService.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArticleService.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticlesController : ControllerBase
{
    private readonly IArticleReadRepository _readRepository;
    private readonly IArticleWriteRepository _writeRepository;
    private readonly IArticleCache _cache;
    private readonly IOptionsMonitor<LocationConfig> _locations;
    private readonly ILogger<ArticlesController> _logger;

    private Location? GetLocationFromCode(string locationCode)
    {
        locationCode = locationCode.ToUpper();
        Location? location = _locations.CurrentValue.Locations.FirstOrDefault(l => l.Code == locationCode && l.Enabled);
        
        if (location == null)
        {
            _logger.LogWarning("Unknown location: {LocationCode}", locationCode);
        }
        
        return location;
    }

    public ArticlesController(
        IArticleReadRepository readRepository,
        IArticleWriteRepository writeRepository,
        IArticleCache cache,
        IOptionsMonitor<LocationConfig> locations,
        ILogger<ArticlesController> logger)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _cache = cache;
        _locations = locations;
        _logger = logger;
    }

    [HttpGet("{locationCode}")]
    public async Task<ActionResult<IEnumerable<Article>>> GetAll(string locationCode)
    {
        Location? location = GetLocationFromCode(locationCode);
        if (location == null)
        {
            return BadRequest($"Unknown location '{locationCode}'.");
        }
        
        IList<Article> cachedArticles = await _cache.GetArticlesAsync(location.Code);
        if (cachedArticles.Any())
        {
            return Ok(cachedArticles);
        }

        return Ok(await _readRepository.GetAllAsync(location.Code));
    }

    // Exactly what a front page shows, composed in one query and cached as a whole. The client
    // passes its own layout: the reader front page asks for top = 1 hero + 5 popular,
    // perSection = 3 cards (GAF: Defer Fetching - fewer rows) and the number of body text
    // characters it displays (GAF: fewer fields; leave excerpt out for the whole text).
    [HttpGet("{location}/frontpage")]
    public async Task<ActionResult<IEnumerable<Article>>> GetFrontPage(
        string location,
        [FromQuery] int top = 10,
        [FromQuery] int perSection = 0,
        [FromQuery] string? section = null,
        [FromQuery] int? excerpt = null,
        [FromQuery] int? heroExcerpt = null)
    {
        Location? locationObj = GetLocationFromCode(location);
        if (locationObj == null)
        {
            return BadRequest($"Unknown location '{location}'.");
        }

        var query = new FrontPageQuery(top, perSection, section, excerpt, heroExcerpt);
        if (query.Validate() is { } error)
        {
            return BadRequest(error);
        }

        IList<Article>? cached = await _cache.GetFrontPageAsync(locationObj.Code, query);
        if (cached is not null)
        {
            return Ok(cached);
        }

        IList<Article> articles = [.. await _readRepository.GetFrontPageAsync(locationObj.Code, query)];
        await _cache.StoreFrontPageAsync(locationObj.Code, query, articles);
        return Ok(articles);
    }

    [HttpGet("{location}/{id:guid}")]
    public async Task<ActionResult<Article>> GetById(string location, Guid id)
    {
        Location? locationObj = GetLocationFromCode(location);
        if (locationObj == null)
        {
            return BadRequest($"Unknown location '{location}'.");
        }
        
        Article? cachedArticle = await _cache.GetArticleByIdAsync(locationObj.Code, id);
        if (cachedArticle != null)
        {
            return Ok(cachedArticle);
        }

        Article? article = await _readRepository.GetByIdAsync(locationObj.Code, id);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpPost("{locationCode}")]
    public async Task<ActionResult<Article>> Create(string locationCode, UpsertArticleRequest request)
    {
        Location? location = GetLocationFromCode(locationCode);
        if (location == null)
        {
            return BadRequest($"Unknown location '{locationCode}'.");
        }
        
        Article article = await _writeRepository.CreateAsync(location.Code, request);
        await _cache.RefreshArticleAsync(location.Code, article.Id);
        return CreatedAtAction(nameof(GetById), new { locationCode = location.Code, id = article.Id }, article);
    }

    [HttpPut("{locationCode}/{id:guid}")]
    public async Task<IActionResult> Update(string locationCode, Guid id, UpsertArticleRequest request)
    {
        Location? location = GetLocationFromCode(locationCode);
        if (location == null)
        {
            return BadRequest($"Unknown location '{locationCode}'.");
        }
        
        bool updated = await _writeRepository.UpdateAsync(location.Code, id, request);
        if (updated)
        {
            await _cache.RefreshArticleAsync(location.Code, id);
        }
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{locationCode}/{id:guid}")]
    public async Task<IActionResult> Delete(string locationCode, Guid id)
    {
        Location? location = GetLocationFromCode(locationCode);
        if (location == null)
        {
            return BadRequest($"Unknown location '{locationCode}'.");
        }
        
        bool deleted = await _writeRepository.DeleteAsync(location.Code, id);
        if (deleted)
        {
            await _cache.RemoveArticleAsync(location.Code, id);
        }
        return deleted ? NoContent() : NotFound();
    }
}
