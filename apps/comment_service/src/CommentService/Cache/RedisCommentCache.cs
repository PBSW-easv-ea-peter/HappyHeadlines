using System.Text.Json;
using CommentService.Models;
using StackExchange.Redis;

namespace CommentService.Cache;

// CommentCache in its own Redis instance, filled on a cache miss and limited to the
// comments of the N most recently accessed articles (LRU eviction).
//
// Redis' own LRU (maxmemory-policy) evicts by memory, not by article count, so the LRU
// is kept here:
//   comments:{location}:{articleId}  list   - sentinel + one JSON comment per element
//   comments:lru                     zset   - article keys scored by last access
//   comments:lru:size                string - number of members in comments:lru
//   comments:lru:clock               string - access counter used as the zset score
//
// The cache dashboard computes the hit ratio from Redis' keyspace_hits/keyspace_misses,
// which count every read lookup. So each read does exactly one lookup (the LRANGE) and
// all LRU bookkeeping uses write commands only - that's also why the size is a counter
// instead of a ZCARD. The scripts run atomically, so concurrent requests from several
// CommentService instances can't corrupt the LRU.
public class RedisCommentCache : ICommentCache
{
    private const string KeyPrefix = "comments:";
    private const string LruKey = "comments:lru";
    private const string LruSizeKey = "comments:lru:size";
    private const string LruClockKey = "comments:lru:clock";

    // First element of every cached list. Redis deletes empty lists, so without it an
    // article with no comments couldn't be cached and would miss on every request.
    private const string Sentinel = "#";

    // Marks the article as most recently used. A new article grows the LRU, and the
    // least recently used articles are evicted until it fits the capacity again.
    private const string TouchFunction = """
        local function touch(article, lru, size, clock, capacity)
            local tick = redis.call('INCR', clock)
            if redis.call('ZADD', lru, tick, article) == 1 then
                local count = redis.call('INCR', size)
                while count > capacity do
                    local evicted = redis.call('ZPOPMIN', lru)
                    if #evicted == 0 then
                        redis.call('SET', size, 0)
                        break
                    end
                    redis.call('DEL', evicted[1])
                    count = redis.call('DECR', size)
                end
            end
        end
        """;

    private const string GetScript = TouchFunction + """

        local items = redis.call('LRANGE', KEYS[1], 0, -1)
        if #items == 0 then
            return false
        end
        touch(KEYS[1], KEYS[2], KEYS[3], KEYS[4], tonumber(ARGV[1]))
        return items
        """;

    // ARGV[2..] is the sentinel followed by the comments. Pushed in chunks because Lua's
    // unpack() is limited by the stack size.
    private const string SetScript = TouchFunction + """

        redis.call('DEL', KEYS[1])
        for i = 2, #ARGV, 500 do
            redis.call('RPUSH', KEYS[1], unpack(ARGV, i, math.min(i + 499, #ARGV)))
        end
        touch(KEYS[1], KEYS[2], KEYS[3], KEYS[4], tonumber(ARGV[1]))
        """;

    private readonly IConnectionMultiplexer _redis;
    private readonly int _capacity;
    private readonly ILogger<RedisCommentCache> _logger;

    public RedisCommentCache(IConnectionMultiplexer redis, IConfiguration configuration, ILogger<RedisCommentCache> logger)
    {
        _redis = redis;
        _capacity = configuration.GetValue("Cache:Capacity", 30);
        _logger = logger;
    }

    public async Task<IReadOnlyList<CommentDto>?> GetAsync(string articleLocation, long articleId)
    {
        try
        {
            var result = await _redis.GetDatabase().ScriptEvaluateAsync(
                GetScript, LruKeys(articleLocation, articleId), [_capacity]);

            if (result.IsNull)
            {
                return null;
            }

            return ((RedisValue[])result!)
                .Skip(1) // sentinel
                .Select(value => JsonSerializer.Deserialize<CommentDto>(value.ToString())!)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CommentCache read failed for article {ArticleId} in {Location}. Falling back to the database.",
                articleId, articleLocation);
            return null;
        }
    }

    public async Task SetAsync(string articleLocation, long articleId, IReadOnlyList<CommentDto> comments)
    {
        RedisValue[] values =
        [
            _capacity,
            Sentinel,
            .. comments.Select(comment => (RedisValue)JsonSerializer.Serialize(comment))
        ];

        try
        {
            await _redis.GetDatabase().ScriptEvaluateAsync(SetScript, LruKeys(articleLocation, articleId), values);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CommentCache fill failed for article {ArticleId} in {Location}.",
                articleId, articleLocation);
        }
    }

    public async Task AppendIfCachedAsync(string articleLocation, long articleId, CommentDto comment)
    {
        var db = _redis.GetDatabase();
        var key = ArticleKey(articleLocation, articleId);

        try
        {
            // RPUSHX only pushes to an existing list, i.e. when the article is cached.
            await db.ListRightPushAsync(key, JsonSerializer.Serialize(comment), When.Exists);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CommentCache write-through failed for article {ArticleId} in {Location}. Dropping the cached entry.",
                articleId, articleLocation);

            // A cached list without the new comment would hide it until eviction. Best
            // effort: if Redis is down this fails too, and the LRU entry left behind is
            // harmless (it is evicted like any other).
            try
            {
                await db.KeyDeleteAsync(key);
            }
            catch (Exception deleteEx)
            {
                _logger.LogWarning(deleteEx, "Could not drop cached comments for article {ArticleId} in {Location}.",
                    articleId, articleLocation);
            }
        }
    }

    private static RedisKey ArticleKey(string articleLocation, long articleId) =>
        $"{KeyPrefix}{articleLocation}:{articleId}";

    private static RedisKey[] LruKeys(string articleLocation, long articleId) =>
        [ArticleKey(articleLocation, articleId), LruKey, LruSizeKey, LruClockKey];
}
