using Messaging.Events;
using Messaging.Exchanges;
using Messaging.RoutingKeys;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Moq;
using PublishService.Features.Publish;
using PublishService.Shared;
using PublishService.Shared.Exceptions;
using PublishService.Shared.External;
using PublishService.Shared.Models;
using RabbitMQ.Client.Exceptions;
using Xunit;

namespace PublishService.Tests.Features;

public class PublishDraftHandlerTests
{
    private static readonly Guid DraftId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly Mock<IHttpDraftClient> _draftClient = new();
    private readonly Mock<IRabbitMqService> _rabbitMq = new();
    private readonly PublishDraftHandler _handler;

    public PublishDraftHandlerTests()
    {
        _handler = new PublishDraftHandler(_draftClient.Object, Mock.Of<ILogger<PublishDraftHandler>>(), _rabbitMq.Object);
        _draftClient.Setup(c => c.MarkPublishedAsync(DraftId)).ReturnsAsync(true);
    }

    private static DraftDTO MakeDraft(DraftStatus status) => new()
    {
        Id = DraftId,
        JournalistName = "Ava Martinez",
        SectionName = "Technology",
        Title = "Title",
        Location = "EU",
        CreatedDate = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc),
        BreadText = "Body",
        Status = status
    };

    private void VerifyNothingPublished()
    {
        _rabbitMq.Verify(r => r.PublishAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _draftClient.Verify(c => c.MarkPublishedAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_ApprovedDraft_QueuesEventAndMarksDraftPublished()
    {
        _draftClient.Setup(c => c.GetAsync(DraftId)).ReturnsAsync(MakeDraft(DraftStatus.Approved));
        PublishedArticleEvent? published = null;
        _rabbitMq
            .Setup(r => r.PublishAsync(It.IsAny<object>(), PublishedArticlesExchange.Name, PublishedArticleRKeys.ArticlePublished))
            .Callback((object message, string _, string _) => published = (PublishedArticleEvent)message)
            .Returns(Task.CompletedTask);

        var result = await _handler.PublishAsync(DraftId);

        var accepted = Assert.IsType<Accepted<PublishDraftResponse>>(result);
        Assert.NotNull(published);
        Assert.Equal(DraftId, published.DraftId);
        Assert.Equal("Ava Martinez", published.JournalistName);
        Assert.Equal("Technology", published.SectionName);
        Assert.Equal("EU", published.Location);
        Assert.Equal(published.Id, accepted.Value!.EventId);
        _draftClient.Verify(c => c.MarkPublishedAsync(DraftId), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_QueuesEventBeforeMarkingDraft()
    {
        // Marking first could leave a Published draft without an article if queueing failed.
        var calls = new List<string>();
        _draftClient.Setup(c => c.GetAsync(DraftId)).ReturnsAsync(MakeDraft(DraftStatus.Approved));
        _rabbitMq
            .Setup(r => r.PublishAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() => calls.Add("queue"))
            .Returns(Task.CompletedTask);
        _draftClient.Setup(c => c.MarkPublishedAsync(DraftId)).Callback(() => calls.Add("mark")).ReturnsAsync(true);

        await _handler.PublishAsync(DraftId);

        Assert.Equal(["queue", "mark"], calls);
    }

    [Fact]
    public async Task PublishAsync_UnknownDraft_ReturnsNotFound()
    {
        _draftClient.Setup(c => c.GetAsync(DraftId)).ReturnsAsync((DraftDTO?)null);

        var result = await _handler.PublishAsync(DraftId);

        Assert.IsType<NotFound>(result);
        VerifyNothingPublished();
    }

    [Theory]
    [InlineData(DraftStatus.WorkInProgress)]
    [InlineData(DraftStatus.PendingApproval)]
    [InlineData(DraftStatus.Published)]
    [InlineData(DraftStatus.Archived)]
    public async Task PublishAsync_DraftNotApproved_ReturnsConflict(DraftStatus status)
    {
        _draftClient.Setup(c => c.GetAsync(DraftId)).ReturnsAsync(MakeDraft(status));

        var result = await _handler.PublishAsync(DraftId);

        Assert.IsType<Conflict<string>>(result);
        VerifyNothingPublished();
    }

    [Fact]
    public async Task PublishAsync_DraftServiceDown_ThrowsInfrastructureException()
    {
        _draftClient.Setup(c => c.GetAsync(DraftId)).ThrowsAsync(new HttpRequestException("refused"));

        await Assert.ThrowsAsync<InfrastructureException>(() => _handler.PublishAsync(DraftId));
        VerifyNothingPublished();
    }

    [Fact]
    public async Task PublishAsync_BrokerDown_ThrowsAndLeavesDraftApproved()
    {
        _draftClient.Setup(c => c.GetAsync(DraftId)).ReturnsAsync(MakeDraft(DraftStatus.Approved));
        _rabbitMq
            .Setup(r => r.PublishAsync(It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new BrokerUnreachableException(new Exception("refused")));

        await Assert.ThrowsAsync<InfrastructureException>(() => _handler.PublishAsync(DraftId));
        _draftClient.Verify(c => c.MarkPublishedAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_MarkingFails_ThrowsSoTheClientCanRetry()
    {
        _draftClient.Setup(c => c.GetAsync(DraftId)).ReturnsAsync(MakeDraft(DraftStatus.Approved));
        _draftClient.Setup(c => c.MarkPublishedAsync(DraftId)).ThrowsAsync(new HttpRequestException("refused"));

        var ex = await Assert.ThrowsAsync<InfrastructureException>(() => _handler.PublishAsync(DraftId));
        Assert.Contains("again", ex.Message);
    }

    [Fact]
    public async Task PublishAsync_DraftServiceRefusesTransition_StillAccepted()
    {
        // The event is already queued, so the article will appear either way.
        _draftClient.Setup(c => c.GetAsync(DraftId)).ReturnsAsync(MakeDraft(DraftStatus.Approved));
        _draftClient.Setup(c => c.MarkPublishedAsync(DraftId)).ReturnsAsync(false);

        var result = await _handler.PublishAsync(DraftId);

        Assert.IsType<Accepted<PublishDraftResponse>>(result);
    }
}
