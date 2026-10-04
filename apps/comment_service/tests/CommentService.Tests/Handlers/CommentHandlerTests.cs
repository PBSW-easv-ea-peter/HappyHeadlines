using CommentService.Cache;
using CommentService.Handlers;
using CommentService.Models;
using CommentService.Profanity;
using CommentService.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CommentService.Tests.Handlers;

public class CommentHandlerTests
{
    private static readonly Guid TestArticleId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private readonly Mock<ICommentRepository> _repository = new();
    private readonly Mock<IProfanityClient> _profanityClient = new();
    private readonly Mock<ICommentCache> _cache = new();
    private readonly CommentHandler _handler;

    public CommentHandlerTests()
    {
        _handler = new CommentHandler(_repository.Object, _profanityClient.Object, _cache.Object, Mock.Of<ILogger<CommentHandler>>());

        _repository
            .Setup(r => r.CreateAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<PostCommentRequest>(), It.IsAny<CommentStatus>()))
            .ReturnsAsync((string location, Guid articleId, PostCommentRequest request, CommentStatus status) => new CommentEntity
            {
                Id = 1,
                ArticleId = articleId,
                ArticleLocation = location,
                AuthorName = request.AuthorName,
                Text = request.Text,
                CreatedDate = DateTimeOffset.UtcNow,
                Status = status
            });
    }

    [Fact]
    public async Task PostAsync_CleanText_IsApproved()
    {
        _profanityClient
            .Setup(c => c.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfanityCheckResult(BannedWords: [], Unavailable: false));

        var request = new PostCommentRequest { AuthorName = "Alice", Text = "This is a nice comment" };

        var (comment, status) = await _handler.PostAsync("EU", TestArticleId, request);

        Assert.Equal(CommentStatus.Approved, status);
        Assert.Equal(CommentStatus.Approved, comment.Status);
    }

    [Fact]
    public async Task PostAsync_ProfaneText_IsRejected()
    {
        _profanityClient
            .Setup(c => c.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfanityCheckResult(BannedWords: ["idiot"], Unavailable: false));

        var request = new PostCommentRequest { AuthorName = "Alice", Text = "You are an idiot" };

        var (_, status) = await _handler.PostAsync("EU", TestArticleId, request);

        Assert.Equal(CommentStatus.Rejected, status);
    }

    [Fact]
    public async Task PostAsync_ProfanityServiceUnavailable_IsPendingProfanityCheck()
    {
        _profanityClient
            .Setup(c => c.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfanityCheckResult(BannedWords: [], Unavailable: true));

        var request = new PostCommentRequest { AuthorName = "Alice", Text = "Doesn't matter" };

        var (_, status) = await _handler.PostAsync("EU", TestArticleId, request);

        Assert.Equal(CommentStatus.PendingProfanityCheck, status);
    }

    [Fact]
    public async Task PostAsync_CallsProfanityClientExactlyOnceWithFullText()
    {
        _profanityClient
            .Setup(c => c.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfanityCheckResult(BannedWords: [], Unavailable: false));

        var request = new PostCommentRequest { AuthorName = "Alice", Text = "one two three four" };

        await _handler.PostAsync("EU", TestArticleId, request);

        // This is the core of today's change: one call with the whole text,
        // not one call per word.
        _profanityClient.Verify(c => c.CheckAsync(request.Text, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PostAsync_PersistsClassifiedStatus()
    {
        _profanityClient
            .Setup(c => c.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfanityCheckResult(BannedWords: ["bandit"], Unavailable: false));

        var request = new PostCommentRequest { AuthorName = "Alice", Text = "bandit" };

        await _handler.PostAsync("EU", TestArticleId, request);

        _repository.Verify(r => r.CreateAsync("EU", TestArticleId, request, CommentStatus.Rejected), Times.Once);
    }

    [Fact]
    public async Task GetApprovedAsync_CacheHit_DoesNotQueryDatabase()
    {
        var cached = new List<CommentDto> { new() { Id = 7, ArticleId = TestArticleId, ArticleLocation = "EU", Text = "Cached" } };
        _cache.Setup(c => c.GetAsync("EU", TestArticleId)).ReturnsAsync(cached);

        var comments = await _handler.GetApprovedAsync("EU", TestArticleId);

        Assert.Same(cached, comments);
        _repository.Verify(r => r.GetApprovedByArticleIdAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyList<CommentDto>>()), Times.Never);
    }

    [Fact]
    public async Task GetApprovedAsync_CacheMiss_LoadsFromDatabaseAndFillsCache()
    {
        _cache.Setup(c => c.GetAsync("EU", TestArticleId)).ReturnsAsync((IReadOnlyList<CommentDto>?)null);
        _repository
            .Setup(r => r.GetApprovedByArticleIdAsync("EU", TestArticleId))
            .ReturnsAsync([new CommentEntity { Id = 7, ArticleId = TestArticleId, ArticleLocation = "EU", Text = "From db", Status = CommentStatus.Approved }]);

        var comments = (await _handler.GetApprovedAsync("EU", TestArticleId)).ToList();

        Assert.Equal(7, Assert.Single(comments).Id);
        _cache.Verify(c => c.SetAsync("EU", TestArticleId, It.Is<IReadOnlyList<CommentDto>>(list => list.Count == 1 && list[0].Id == 7)), Times.Once);
    }

    [Fact]
    public async Task GetApprovedAsync_CacheMissWithoutComments_CachesEmptyList()
    {
        // Otherwise every read of an article without comments would hit the database.
        _cache.Setup(c => c.GetAsync("EU", TestArticleId)).ReturnsAsync((IReadOnlyList<CommentDto>?)null);
        _repository.Setup(r => r.GetApprovedByArticleIdAsync("EU", TestArticleId)).ReturnsAsync([]);

        await _handler.GetApprovedAsync("EU", TestArticleId);

        _cache.Verify(c => c.SetAsync("EU", TestArticleId, It.Is<IReadOnlyList<CommentDto>>(list => list.Count == 0)), Times.Once);
    }

    [Fact]
    public async Task PostAsync_Approved_WritesThroughToCache()
    {
        _profanityClient
            .Setup(c => c.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfanityCheckResult(BannedWords: [], Unavailable: false));

        var request = new PostCommentRequest { AuthorName = "Alice", Text = "This is a nice comment" };

        var (comment, _) = await _handler.PostAsync("EU", TestArticleId, request);

        _cache.Verify(c => c.AppendIfCachedAsync("EU", TestArticleId, comment), Times.Once);
    }

    [Theory]
    [InlineData(false, "idiot")] // rejected
    [InlineData(true)]           // pending profanity check
    public async Task PostAsync_NotApproved_IsNotCached(bool unavailable, params string[] bannedWords)
    {
        _profanityClient
            .Setup(c => c.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfanityCheckResult(BannedWords: bannedWords, Unavailable: unavailable));

        var request = new PostCommentRequest { AuthorName = "Alice", Text = "You are an idiot" };

        await _handler.PostAsync("EU", TestArticleId, request);

        _cache.Verify(c => c.AppendIfCachedAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CommentDto>()), Times.Never);
    }
}
