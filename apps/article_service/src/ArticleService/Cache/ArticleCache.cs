using ArticleService.Models;
using ArticleService.Repositories;
using StackExchange.Redis;
using System.Text.Json;

namespace ArticleService.Cache;

public class ArticleCache : IArticleCache
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IArticleReadRepository _readRepository;
    private readonly ILogger<ArticleCache> _logger;

    private const string ArticlesKeyPrefix = "articles:";
    private const string ArticleKeyPrefix = "article:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(14);
    
    public ArticleCache(
        IConnectionMultiplexer redis,
        IArticleReadRepository readRepository,
        ILogger<ArticleCache> logger)
    {
        _redis = redis;
        _readRepository = readRepository;
        _logger = logger;
    }

    public async Task<IList<Article>> GetArticlesAsync(string location)
    {
        string key = $"{ArticlesKeyPrefix}{location}";

        RedisValue cached = await TryGetAsync(key);
        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<IList<Article>>(cached.ToString()) ?? [];
        }

        return [];
    }

    public async Task<Article?> GetArticleByIdAsync(string location, Guid id)
    {
        string key = $"{ArticleKeyPrefix}{location}:{id}";

        RedisValue cached = await TryGetAsync(key);
        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<Article>(cached.ToString());
        }

        return null;
    }

    // A failing cache must never fail the request: if Redis is down or slow, the read counts
    // as a miss and ArticlesController falls back to the database.
    private async Task<RedisValue> TryGetAsync(string key)
    {
        try
        {
            return await _redis.GetDatabase().StringGetAsync(key);
        }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException)
        {
            _logger.LogWarning("ArticleCache read failed ({ExceptionType}) - falling back to the database",
                ex.GetType().Name);
            return RedisValue.Null;
        }
    }

    public async Task RefreshCacheAsync(params string[] locations)
    {
        _logger.LogInformation("Starting full cache refresh of locations: {locations}", string.Join(", ", locations));

        foreach (string location in locations)
        {
            try
            {
                IList<Article> articles = [.. await _readRepository.GetAllAsync(location)];
                
                IDatabase db = _redis.GetDatabase();
                string articlesKey = $"{ArticlesKeyPrefix}{location}";
                string serialized = JsonSerializer.Serialize(articles);
                await db.StringSetAsync(articlesKey, serialized, CacheDuration);
                
                foreach (Article article in articles)
                {
                    string articleKey = $"{ArticleKeyPrefix}{location}:{article.Id}";
                    string articleSerialized = JsonSerializer.Serialize(article);
                    await db.StringSetAsync(articleKey, articleSerialized, CacheDuration);
                }

                _logger.LogInformation("Refreshed cache for location: {Location}", location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh cache for location: {Location}", location);
            }
        }

        _logger.LogInformation("Full cache refresh completed");
    }

    public async Task RefreshArticleAsync(string location, Guid id)
    {
        try
        {
            Article? article = await _readRepository.GetByIdAsync(location, id);
            if (article != null)
            {
                IDatabase db = _redis.GetDatabase();
                string key = $"{ArticleKeyPrefix}{location}:{id}";
                string serialized = JsonSerializer.Serialize(article);
                await db.StringSetAsync(key, serialized, CacheDuration);
                _logger.LogDebug("Refreshed single article cache for {Location}:{Id}", location, id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh article cache for {Location}:{Id}", location, id);
        }
    }

    public async Task InvalidateArticleAsync(string location, Guid id)
    {
        try
        {
            IDatabase db = _redis.GetDatabase();
            string key = $"{ArticleKeyPrefix}{location}:{id}";
            await db.KeyDeleteAsync(key);
            _logger.LogDebug("Invalidated article cache for {Location}:{Id}", location, id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate article cache for {Location}:{Id}", location, id);
        }
    }
}