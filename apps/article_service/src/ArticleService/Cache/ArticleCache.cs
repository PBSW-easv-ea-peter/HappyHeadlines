using ArticleService.Models;
using ArticleService.Repositories;
using StackExchange.Redis;
using System.Text.Json;

namespace ArticleService.Cache;

// Each article is stored once, and the front page list is an index of article ids:
//   article:{location}:{id}       string - the article as JSON
//   articles:{location}:index     zset   - article ids scored by publish date (newest first)
//
// So editing an article only rewrites its own key, and deleting one only removes it from
// the index - the front page is up to date without rebuilding the whole list.
public class ArticleCache : IArticleCache
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IArticleReadRepository _readRepository;
    private readonly ILogger<ArticleCache> _logger;

    private const string IndexKeyPrefix = "articles:";
    private const string ArticleKeyPrefix = "article:";
    // Only articles published within this window are cached (the assignment's "latest 14 days").
    private static readonly TimeSpan CacheWindow = TimeSpan.FromDays(14);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(14);

    // Replaces the whole index atomically, so readers never see a half-built front page.
    // ARGV[1] is the TTL in seconds, ARGV[2..] are score/id pairs. Added in chunks because
    // Lua's unpack() is limited by the stack size.
    private const string ReplaceIndexScript = """
        redis.call('DEL', KEYS[1])
        for i = 2, #ARGV, 1000 do
            redis.call('ZADD', KEYS[1], unpack(ARGV, i, math.min(i + 999, #ARGV)))
        end
        if #ARGV > 1 then
            redis.call('EXPIRE', KEYS[1], ARGV[1])
        end
        """;

    // Writes the article and puts it on the front page. The index is only touched if it
    // exists: after a Redis restart an edit must not create an index holding one article,
    // which would look like a front page hit. The next full refresh rebuilds it.
    private const string UpsertScript = """
        redis.call('SET', KEYS[1], ARGV[1], 'EX', ARGV[2])
        if redis.call('EXISTS', KEYS[2]) == 1 then
            redis.call('ZADD', KEYS[2], ARGV[3], ARGV[4])
        end
        """;

    private const string RemoveScript = """
        redis.call('ZREM', KEYS[2], ARGV[1])
        redis.call('DEL', KEYS[1])
        """;

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
        try
        {
            IDatabase db = _redis.GetDatabase();
            RedisValue[] ids = await db.SortedSetRangeByRankAsync(IndexKey(location), order: Order.Descending);
            if (ids.Length == 0)
            {
                return [];
            }

            RedisValue[] values = await db.StringGetAsync([.. ids.Select(id => ArticleKey(location, id.ToString()))]);

            List<Article> articles = new(values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].HasValue)
                {
                    articles.Add(JsonSerializer.Deserialize<Article>(values[i].ToString())!);
                    continue;
                }

                // The id is on the front page but its key is gone (e.g. evicted by Redis).
                // Fetch just that article instead of failing the whole front page.
                Article? article = await _readRepository.GetByIdAsync(location, Guid.Parse(ids[i].ToString()));
                if (article != null)
                {
                    articles.Add(article);
                }
            }

            return articles;
        }
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException)
        {
            _logger.LogWarning("ArticleCache read failed ({ExceptionType}) - falling back to the database",
                ex.GetType().Name);
            return [];
        }
    }

    public async Task<Article?> GetArticleByIdAsync(string location, Guid id)
    {
        RedisValue cached = await TryGetAsync(ArticleKey(location, id.ToString()));
        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<Article>(cached.ToString());
        }

        return null;
    }

    // A failing cache must never fail the request: if Redis is down or slow, the read counts
    // as a miss and ArticlesController falls back to the database.
    private async Task<RedisValue> TryGetAsync(RedisKey key)
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
                DateTimeOffset since = DateTimeOffset.UtcNow - CacheWindow;
                IList<Article> articles = [.. await _readRepository.GetPublishedSinceAsync(location, since)];

                IDatabase db = _redis.GetDatabase();

                // Articles first, then the index, so the index never points at a missing key.
                foreach (Article article in articles)
                {
                    string articleSerialized = JsonSerializer.Serialize(article);
                    await db.StringSetAsync(ArticleKey(location, article.Id.ToString()), articleSerialized, CacheDuration);
                }

                RedisValue[] args =
                [
                    (long)CacheDuration.TotalSeconds,
                    .. articles.SelectMany(article => new RedisValue[] { Score(article), article.Id.ToString() })
                ];
                await db.ScriptEvaluateAsync(ReplaceIndexScript, [IndexKey(location)], args);

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
            if (article == null || !IsInCacheWindow(article))
            {
                // Deleted, unpublished or too old - it no longer belongs on the front page.
                await RemoveArticleAsync(location, id);
                return;
            }

            await _redis.GetDatabase().ScriptEvaluateAsync(
                UpsertScript,
                [ArticleKey(location, id.ToString()), IndexKey(location)],
                [JsonSerializer.Serialize(article), (long)CacheDuration.TotalSeconds, Score(article), id.ToString()]);
            _logger.LogDebug("Refreshed single article cache for {Location}:{Id}", location, id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh article cache for {Location}:{Id}", location, id);
        }
    }

    public async Task RemoveArticleAsync(string location, Guid id)
    {
        try
        {
            await _redis.GetDatabase().ScriptEvaluateAsync(
                RemoveScript,
                [ArticleKey(location, id.ToString()), IndexKey(location)],
                [id.ToString()]);
            _logger.LogDebug("Removed article {Location}:{Id} from the cache", location, id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove article {Location}:{Id} from the cache", location, id);
        }
    }

    private static bool IsInCacheWindow(Article article) =>
        article.PublishDate is { } published
        && published >= DateTimeOffset.UtcNow - CacheWindow
        && published <= DateTimeOffset.UtcNow;

    private static long Score(Article article) => article.PublishDate!.Value.ToUnixTimeMilliseconds();

    private static RedisKey IndexKey(string location) => $"{IndexKeyPrefix}{location}:index";

    private static RedisKey ArticleKey(string location, string id) => $"{ArticleKeyPrefix}{location}:{id}";
}
