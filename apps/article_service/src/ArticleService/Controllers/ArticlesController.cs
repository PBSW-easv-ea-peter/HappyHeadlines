using ArticleService.Cache;
using ArticleService.Models;
using ArticleService.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
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

    private Location GetLocationFromCode(string locationCode)
    {
        locationCode = locationCode.ToUpper();
        Location location = _locations.CurrentValue.Locations.FirstOrDefault(l => l.Code == locationCode && l.Enabled)
                             ?? throw new ArgumentException($"Unknown location '{locationCode}'");

        return location;
    }

    public ArticlesController(
        IArticleReadRepository readRepository,
        IArticleWriteRepository writeRepository,
        IArticleCache cache,
        IOptionsMonitor<LocationConfig> locations)
    {
        _readRepository = readRepository;
        _writeRepository = writeRepository;
        _cache = cache;
        _locations = locations;
    }

    [HttpGet("{locationCode}")]
    public async Task<ActionResult<IEnumerable<Article>>> GetAll(string locationCode)
    {
        Location location = GetLocationFromCode(locationCode);
        
        IList<Article> cachedArticles = await _cache.GetArticlesAsync(location.Code);
        if (cachedArticles.Any())
        {
            return Ok(cachedArticles);
        }

        return Ok(await _readRepository.GetAllAsync(location.Code));
    }

    [HttpGet("{locationCode}/{id:guid}")]
    public async Task<ActionResult<Article>> GetById(string locationCode, Guid id)
    {
        Location location = GetLocationFromCode(locationCode);
        
        Article? cachedArticle = await _cache.GetArticleByIdAsync(location.Code, id);
        if (cachedArticle != null)
        {
            return Ok(cachedArticle);
        }

        Article? article = await _readRepository.GetByIdAsync(location.Code, id);
        return article is null ? NotFound() : Ok(article);
    }

    [HttpPost("{locationCode}")]
    public async Task<ActionResult<Article>> Create(string locationCode, UpsertArticleRequest request)
    {
        Location location = GetLocationFromCode(locationCode);
        
        Article article = await _writeRepository.CreateAsync(location.Code, request);
        await _cache.RefreshArticleAsync(location.Code, article.Id);
        return CreatedAtAction(nameof(GetById), new { locationCode = location.Code, id = article.Id }, article);
    }

    [HttpPut("{locationCode}/{id:guid}")]
    public async Task<IActionResult> Update(string locationCode, Guid id, UpsertArticleRequest request)
    {
        Location location = GetLocationFromCode(locationCode);
        
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
        Location location = GetLocationFromCode(locationCode);
        
        bool deleted = await _writeRepository.DeleteAsync(location.Code, id);
        if (deleted)
        {
            await _cache.RemoveArticleAsync(location.Code, id);
        }
        return deleted ? NoContent() : NotFound();
    }
}
