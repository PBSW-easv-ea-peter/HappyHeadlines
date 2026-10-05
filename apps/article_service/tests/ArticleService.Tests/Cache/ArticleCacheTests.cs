using System.Text.Json;
using ArticleService.Cache;
using ArticleService.Models;
using ArticleService.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace ArticleService.Tests.Cache;

// Redis being down must turn cache reads into misses, so ArticlesController falls back to
// the database instead of returning 500.
public class ArticleCacheTests
{
    private readonly Mock<IDatabase> _db = new();
    private readonly Mock<IArticleReadRepository> _readRepository = new();
    private readonly ArticleCache _cache;

    public ArticleCacheTests()
    {
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_db.Object);
        _cache = new ArticleCache(redis.Object, _readRepository.Object, Mock.Of<ILogger<ArticleCache>>());
    }

    private void RedisThrows(Exception ex)
    {
        _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ThrowsAsync(ex);
        _db.Setup(d => d.SortedSetRangeByRankAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<Order>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(ex);
    }

    private void IndexContains(params Guid[] ids) =>
        _db.Setup(d => d.SortedSetRangeByRankAsync("articles:GO:index", It.IsAny<long>(), It.IsAny<long>(), Order.Descending, It.IsAny<CommandFlags>()))
            .ReturnsAsync([.. ids.Select(id => (RedisValue)id.ToString())]);

    // Checked via Invocations so the tests don't depend on which ScriptEvaluateAsync overload is used.
    private IEnumerable<(string Script, RedisKey[] Keys, RedisValue[] Values)> ScriptCalls() =>
        _db.Invocations
            .Where(i => i.Method.Name == nameof(IDatabase.ScriptEvaluateAsync) && i.Arguments[0] is string)
            .Select(i => ((string)i.Arguments[0], (RedisKey[])i.Arguments[1], (RedisValue[])i.Arguments[2]));

    private static Article Published(Guid id, string title, int daysAgo = 1) =>
        new() { Id = id, Title = title, PublishDate = DateTimeOffset.UtcNow.AddDays(-daysAgo) };

    public static TheoryData<Exception> RedisFailures => new()
    {
        new RedisConnectionException(ConnectionFailureType.UnableToConnect, "No connection is active"),
        new RedisTimeoutException("Timeout performing GET", CommandStatus.Sent),
    };

    [Theory]
    [MemberData(nameof(RedisFailures))]
    public async Task GetArticlesAsync_RedisUnavailable_IsAMiss(Exception failure)
    {
        RedisThrows(failure);

        var articles = await _cache.GetArticlesAsync("GO");

        Assert.Empty(articles);
    }

    [Theory]
    [MemberData(nameof(RedisFailures))]
    public async Task GetArticleByIdAsync_RedisUnavailable_IsAMiss(Exception failure)
    {
        RedisThrows(failure);

        var article = await _cache.GetArticleByIdAsync("GO", default);

        Assert.Null(article);
    }

    [Fact]
    public async Task GetArticlesAsync_NoIndex_IsAMiss()
    {
        IndexContains();

        var articles = await _cache.GetArticlesAsync("GO");

        Assert.Empty(articles);
    }

    [Fact]
    public async Task GetArticlesAsync_Cached_ReturnsArticlesInIndexOrder()
    {
        Guid newest = Guid.NewGuid(), older = Guid.NewGuid();
        IndexContains(newest, older);
        _db.Setup(d => d.StringGetAsync(
                It.Is<RedisKey[]>(keys => keys.SequenceEqual(new RedisKey[] { $"article:GO:{newest}", $"article:GO:{older}" })),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync([JsonSerializer.Serialize(Published(newest, "Newest")), JsonSerializer.Serialize(Published(older, "Older", 2))]);

        var articles = await _cache.GetArticlesAsync("GO");

        Assert.Equal(["Newest", "Older"], articles.Select(a => a.Title));
    }

    [Fact]
    public async Task GetArticlesAsync_ArticleKeyMissing_FetchesThatArticleFromTheDatabase()
    {
        Guid cached = Guid.NewGuid(), evicted = Guid.NewGuid();
        IndexContains(cached, evicted);
        _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync([JsonSerializer.Serialize(Published(cached, "Cached")), RedisValue.Null]);
        _readRepository.Setup(r => r.GetByIdAsync("GO", evicted)).ReturnsAsync(Published(evicted, "From database"));

        var articles = await _cache.GetArticlesAsync("GO");

        Assert.Equal(["Cached", "From database"], articles.Select(a => a.Title));
        _readRepository.Verify(r => r.GetByIdAsync("GO", cached), Times.Never);
    }

    [Fact]
    public async Task RefreshCacheAsync_FillsCacheWithArticlesFromTheLatest14Days()
    {
        var recent = Published(Guid.NewGuid(), "Recent story");
        DateTimeOffset? since = null;
        _readRepository
            .Setup(r => r.GetPublishedSinceAsync("GO", It.IsAny<DateTimeOffset>()))
            .Callback<string, DateTimeOffset>((_, s) => since = s)
            .ReturnsAsync([recent]);

        await _cache.RefreshCacheAsync("GO");

        Assert.NotNull(since);
        Assert.InRange(since.Value, DateTimeOffset.UtcNow.AddDays(-14).AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(-14));
        _readRepository.Verify(r => r.GetAllAsync(It.IsAny<string>()), Times.Never);

        var articleWrite = Assert.Single(_db.Invocations, i =>
            i.Method.Name == nameof(IDatabase.StringSetAsync) && (RedisKey)i.Arguments[0] == $"article:GO:{recent.Id}");
        Assert.Contains("Recent story", ((RedisValue)articleWrite.Arguments[1]).ToString());

        var index = Assert.Single(ScriptCalls());
        Assert.Equal("articles:GO:index", Assert.Single(index.Keys));
        // TTL, then the score/id pair of each article.
        Assert.Equal(recent.PublishDate!.Value.ToUnixTimeMilliseconds(), (long)index.Values[1]);
        Assert.Equal(recent.Id.ToString(), index.Values[2].ToString());
    }

    [Fact]
    public async Task RefreshArticleAsync_EditedArticle_RewritesOnlyThatArticle()
    {
        var edited = Published(Guid.NewGuid(), "Fixed typo");
        _readRepository.Setup(r => r.GetByIdAsync("GO", edited.Id)).ReturnsAsync(edited);

        await _cache.RefreshArticleAsync("GO", edited.Id);

        var upsert = Assert.Single(ScriptCalls());
        Assert.Contains("SET", upsert.Script);
        Assert.Equal(new RedisKey[] { $"article:GO:{edited.Id}", "articles:GO:index" }, upsert.Keys);
        Assert.Contains("Fixed typo", upsert.Values[0].ToString());
        _readRepository.Verify(r => r.GetPublishedSinceAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    public static TheoryData<Article?> NotOnTheFrontPage => new()
    {
        null, // deleted
        new Article { Id = Guid.NewGuid(), PublishDate = null }, // unpublished
        new Article { Id = Guid.NewGuid(), PublishDate = DateTimeOffset.UtcNow.AddDays(1) }, // scheduled
        new Article { Id = Guid.NewGuid(), PublishDate = DateTimeOffset.UtcNow.AddDays(-15) }, // too old
    };

    [Theory]
    [MemberData(nameof(NotOnTheFrontPage))]
    public async Task RefreshArticleAsync_ArticleOutsideTheWindow_IsRemoved(Article? article)
    {
        var id = article?.Id ?? Guid.NewGuid();
        _readRepository.Setup(r => r.GetByIdAsync("GO", id)).ReturnsAsync(article);

        await _cache.RefreshArticleAsync("GO", id);

        var remove = Assert.Single(ScriptCalls());
        Assert.Contains("ZREM", remove.Script);
        Assert.Equal(new RedisKey[] { $"article:GO:{id}", "articles:GO:index" }, remove.Keys);
        Assert.Equal(id.ToString(), Assert.Single(remove.Values).ToString());
    }
}
