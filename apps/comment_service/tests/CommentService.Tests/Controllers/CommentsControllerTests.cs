using CommentService.Controllers;
using CommentService.Handlers;
using CommentService.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CommentService.Tests.Controllers;

public class CommentsControllerTests
{
    private readonly Mock<ICommentHandler> _handler = new();
    private readonly CommentsController _controller;

    public CommentsControllerTests()
    {
        _controller = new CommentsController(_handler.Object);
    }

    [Fact]
    public async Task GetForArticle_UnknownLocation_ReturnsBadRequest()
    {
        var result = await _controller.GetForArticle("XX", 1);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetForArticle_KnownLocation_ReturnsCommentsFromHandler()
    {
        var comments = new List<CommentDto>
        {
            new() { Id = 1, ArticleId = 1, ArticleLocation = "EU", AuthorName = "Alice", Text = "Hi", Status = CommentStatus.Approved }
        };
        _handler.Setup(h => h.GetApprovedAsync("EU", 1)).ReturnsAsync(comments);

        var result = await _controller.GetForArticle("EU", 1);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(comments, ok.Value);
    }

    [Fact]
    public async Task Post_UnknownLocation_ReturnsBadRequest()
    {
        var result = await _controller.Post("XX", 1, new PostCommentRequest { AuthorName = "Alice", Text = "Hi" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Theory]
    [InlineData("", "Text")]
    [InlineData("Alice", "")]
    [InlineData(" ", "Text")]
    public async Task Post_EmptyAuthorOrText_ReturnsBadRequest(string authorName, string text)
    {
        var result = await _controller.Post("EU", 1, new PostCommentRequest { AuthorName = authorName, Text = text });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Post_Approved_ReturnsCreated()
    {
        var comment = new CommentDto { Id = 1, ArticleId = 1, ArticleLocation = "EU", AuthorName = "Alice", Text = "Hi", Status = CommentStatus.Approved };
        _handler.Setup(h => h.PostAsync("EU", 1, It.IsAny<PostCommentRequest>()))
            .ReturnsAsync((comment, CommentStatus.Approved));

        var result = await _controller.Post("EU", 1, new PostCommentRequest { AuthorName = "Alice", Text = "Hi" });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Same(comment, created.Value);
    }

    [Fact]
    public async Task Post_Rejected_StillReturnsCreated()
    {
        // Design decision from this session: a flagged comment is still saved,
        // so it still gets 201 - the caller reads comment.Status to see it was rejected.
        var comment = new CommentDto { Id = 1, ArticleId = 1, ArticleLocation = "EU", AuthorName = "Alice", Text = "bandit", Status = CommentStatus.Rejected };
        _handler.Setup(h => h.PostAsync("EU", 1, It.IsAny<PostCommentRequest>()))
            .ReturnsAsync((comment, CommentStatus.Rejected));

        var result = await _controller.Post("EU", 1, new PostCommentRequest { AuthorName = "Alice", Text = "bandit" });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(CommentStatus.Rejected, ((CommentDto)created.Value!).Status);
    }

    [Fact]
    public async Task Post_PendingProfanityCheck_ReturnsUnprocessableEntity()
    {
        var comment = new CommentDto { Id = 1, ArticleId = 1, ArticleLocation = "EU", AuthorName = "Alice", Text = "Hi", Status = CommentStatus.PendingProfanityCheck };
        _handler.Setup(h => h.PostAsync("EU", 1, It.IsAny<PostCommentRequest>()))
            .ReturnsAsync((comment, CommentStatus.PendingProfanityCheck));

        var result = await _controller.Post("EU", 1, new PostCommentRequest { AuthorName = "Alice", Text = "Hi" });

        Assert.IsType<UnprocessableEntityObjectResult>(result.Result);
    }
}
