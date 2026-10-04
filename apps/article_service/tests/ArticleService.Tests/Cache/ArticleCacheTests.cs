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
    private readonly ArticleCache _cache;

    public ArticleCacheTests()
    {
        var redis = new Mock<IConnectionMultiplexer>();
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_db.Object);
        _cache = new ArticleCache(redis.Object, Mock.Of<IArticleReadRepository>(), Mock.Of<ILogger<ArticleCache>>());
    }

    private void RedisThrows(Exception ex) =>
        _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ThrowsAsync(ex);

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
    public async Task GetArticlesAsync_Cached_ReturnsArticles()
    {
        var cached = JsonSerializer.Serialize(new List<Article> { new() { Title = "Cached story" } });
        _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(cached);

        var articles = await _cache.GetArticlesAsync("GO");

        Assert.Equal("Cached story", Assert.Single(articles).Title);
    }
}
