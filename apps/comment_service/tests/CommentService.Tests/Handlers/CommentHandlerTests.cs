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
    private readonly Mock<ICommentRepository> _repository = new();
    private readonly Mock<IProfanityClient> _profanityClient = new();
    private readonly CommentHandler _handler;

    public CommentHandlerTests()
    {
        _handler = new CommentHandler(_repository.Object, _profanityClient.Object, Mock.Of<ILogger<CommentHandler>>());

        _repository
            .Setup(r => r.CreateAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<PostCommentRequest>(), It.IsAny<CommentStatus>()))
            .ReturnsAsync((string location, long articleId, PostCommentRequest request, CommentStatus status) => new CommentEntity
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
            .ReturnsAsync(new ProfanityCheckResult(BannedWords: [], CircuitOpen: false));

        var request = new PostCommentRequest { AuthorName = "Alice", Text = "This is a nice comment" };

        var (comment, status) = await _handler.PostAsync("EU", 1, request);

        Assert.Equal(CommentStatus.Approved, status);
        Assert.Equal(CommentStatus.Approved, comment.Status);
    }

    [Fact]
    public async Task PostAsync_ProfaneText_IsRejected()
    {
        _profanityClient
            .Setup(c => c.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfanityCheckResult(BannedWords: ["idiot"], CircuitOpen: false));

        var request = new PostCommentRequest { AuthorName = "Alice", Text = "You are an idiot" };

        var (_, status) = await _handler.PostAsync("EU", 1, request);

        Assert.Equal(CommentStatus.Rejected, status);
    }

    [Fact]
    public async Task PostAsync_CircuitOpen_IsPendingProfanityCheck()
    {
        _profanityClient
            .Setup(c => c.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfanityCheckResult(BannedWords: [], CircuitOpen: true));

        var request = new PostCommentRequest { AuthorName = "Alice", Text = "Doesn't matter" };

        var (_, status) = await _handler.PostAsync("EU", 1, request);

        Assert.Equal(CommentStatus.PendingProfanityCheck, status);
    }

    [Fact]
    public async Task PostAsync_CallsProfanityClientExactlyOnceWithFullText()
    {
        _profanityClient
            .Setup(c => c.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfanityCheckResult(BannedWords: [], CircuitOpen: false));

        var request = new PostCommentRequest { AuthorName = "Alice", Text = "one two three four" };

        await _handler.PostAsync("EU", 1, request);

        // This is the core of today's change: one call with the whole text,
        // not one call per word.
        _profanityClient.Verify(c => c.CheckAsync(request.Text, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PostAsync_PersistsClassifiedStatus()
    {
        _profanityClient
            .Setup(c => c.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfanityCheckResult(BannedWords: ["bandit"], CircuitOpen: false));

        var request = new PostCommentRequest { AuthorName = "Alice", Text = "bandit" };

        await _handler.PostAsync("EU", 1, request);

        _repository.Verify(r => r.CreateAsync("EU", 1, request, CommentStatus.Rejected), Times.Once);
    }
}
