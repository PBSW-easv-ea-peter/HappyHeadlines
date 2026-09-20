using DraftService.Handlers;
using DraftService.Models;
using DraftService.Profanity;
using DraftService.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DraftService.Tests.Handlers;

public class DraftHandlerTests
{
    private readonly Mock<IDraftRepository> _repository = new();
    private readonly Mock<IProfanityClient> _profanityClient = new();
    private readonly DraftHandler _handler;

    public DraftHandlerTests()
    {
        _handler = new DraftHandler(_repository.Object, _profanityClient.Object, Mock.Of<ILogger<DraftHandler>>());
    }

    private static Draft MakeDraft(long id, DraftStatus status, string breadtext = "Body") => new()
    {
        Id = id,
        Title = "Title",
        Breadtext = breadtext,
        Location = "EU",
        SectionId = 1,
        CreatedByJournalistId = 1,
        LastEditedByJournalistId = 1,
        Status = status
    };

    [Fact]
    public async Task CreateAsync_UnknownLocation_ReturnsValidationFailed()
    {
        var request = new CreateDraftRequest { JournalistId = 1, Title = "T", Breadtext = "B", Location = "XX", SectionId = 1 };

        var result = await _handler.CreateAsync(request);

        Assert.Equal(DraftActionOutcome.ValidationFailed, result.Outcome);
        _repository.Verify(r => r.CreateAsync(It.IsAny<CreateDraftRequest>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ValidLocation_ReturnsSuccess()
    {
        var request = new CreateDraftRequest { JournalistId = 1, Title = "T", Breadtext = "B", Location = "EU", SectionId = 1 };
        var draft = MakeDraft(1, DraftStatus.WorkInProgress);
        _repository.Setup(r => r.CreateAsync(request)).ReturnsAsync(draft);

        var result = await _handler.CreateAsync(request);

        Assert.Equal(DraftActionOutcome.Success, result.Outcome);
        Assert.Same(draft, result.Draft);
    }

    [Fact]
    public async Task UpdateContentAsync_NotWorkInProgress_ReturnsIllegalTransition()
    {
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDraft(1, DraftStatus.PendingApproval));
        var request = new EditDraftRequest { JournalistId = 1, Title = "T", Breadtext = "B", Location = "EU", SectionId = 1 };

        var result = await _handler.UpdateContentAsync(1, request);

        Assert.Equal(DraftActionOutcome.IllegalTransition, result.Outcome);
        _repository.Verify(r => r.UpdateContentAsync(It.IsAny<long>(), It.IsAny<EditDraftRequest>()), Times.Never);
    }

    [Fact]
    public async Task UpdateContentAsync_WorkInProgress_ReturnsSuccess()
    {
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDraft(1, DraftStatus.WorkInProgress));
        var request = new EditDraftRequest { JournalistId = 1, Title = "New", Breadtext = "B", Location = "EU", SectionId = 1 };
        var updated = MakeDraft(1, DraftStatus.WorkInProgress);
        _repository.Setup(r => r.UpdateContentAsync(1, request)).ReturnsAsync(updated);

        var result = await _handler.UpdateContentAsync(1, request);

        Assert.Equal(DraftActionOutcome.Success, result.Outcome);
        Assert.Same(updated, result.Draft);
    }

    [Fact]
    public async Task SubmitForApprovalAsync_CleanText_SubmitsWithNoFlaggedWords()
    {
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDraft(1, DraftStatus.WorkInProgress, "A perfectly clean sentence."));
        _profanityClient.Setup(c => c.CheckAsync("A perfectly clean sentence.", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfanityCheckResult(BannedWords: [], CircuitOpen: false));
        _repository.Setup(r => r.SubmitForApprovalAsync(1, DraftStatus.WorkInProgress, It.Is<IReadOnlyList<string>>(w => w.Count == 0)))
            .ReturnsAsync(MakeDraft(1, DraftStatus.PendingApproval));

        var result = await _handler.SubmitForApprovalAsync(1);

        Assert.Equal(DraftActionOutcome.Success, result.Outcome);
    }

    [Fact]
    public async Task SubmitForApprovalAsync_ProfaneText_PersistsFlaggedWords()
    {
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDraft(1, DraftStatus.WorkInProgress, "You are stupid."));
        _profanityClient.Setup(c => c.CheckAsync("You are stupid.", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfanityCheckResult(BannedWords: ["stupid"], CircuitOpen: false));
        _repository
            .Setup(r => r.SubmitForApprovalAsync(1, DraftStatus.WorkInProgress, It.Is<IReadOnlyList<string>>(w => w.SequenceEqual(new[] { "stupid" }))))
            .ReturnsAsync(MakeDraft(1, DraftStatus.PendingApproval));

        var result = await _handler.SubmitForApprovalAsync(1);

        Assert.Equal(DraftActionOutcome.Success, result.Outcome);
        _repository.Verify(
            r => r.SubmitForApprovalAsync(1, DraftStatus.WorkInProgress, It.Is<IReadOnlyList<string>>(w => w.SequenceEqual(new[] { "stupid" }))),
            Times.Once);
    }

    [Fact]
    public async Task SubmitForApprovalAsync_ProfanityServiceCircuitOpen_StillSubmitsWithNoFlaggedWords()
    {
        // Fault isolation: ProfanityService being unreachable must not block the author
        // from submitting - see the same principle in CommentHandler.
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDraft(1, DraftStatus.WorkInProgress));
        _profanityClient.Setup(c => c.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfanityCheckResult(BannedWords: [], CircuitOpen: true));
        _repository.Setup(r => r.SubmitForApprovalAsync(1, DraftStatus.WorkInProgress, It.Is<IReadOnlyList<string>>(w => w.Count == 0)))
            .ReturnsAsync(MakeDraft(1, DraftStatus.PendingApproval));

        var result = await _handler.SubmitForApprovalAsync(1);

        Assert.Equal(DraftActionOutcome.Success, result.Outcome);
    }

    [Fact]
    public async Task SubmitForApprovalAsync_NotWorkInProgress_ReturnsIllegalTransitionWithoutCallingProfanityService()
    {
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDraft(1, DraftStatus.Approved));

        var result = await _handler.SubmitForApprovalAsync(1);

        Assert.Equal(DraftActionOutcome.IllegalTransition, result.Outcome);
        _profanityClient.Verify(c => c.CheckAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApproveAsync_FromPendingApproval_ReturnsSuccess()
    {
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDraft(1, DraftStatus.PendingApproval));
        var updated = MakeDraft(1, DraftStatus.Approved);
        _repository.Setup(r => r.ApproveAsync(1, DraftStatus.PendingApproval, 7, "Looks good")).ReturnsAsync(updated);

        var result = await _handler.ApproveAsync(1, new ApproveDraftRequest { JournalistId = 7, Note = "Looks good" });

        Assert.Equal(DraftActionOutcome.Success, result.Outcome);
        Assert.Same(updated, result.Draft);
    }

    [Fact]
    public async Task ApproveAsync_FromWorkInProgress_ReturnsIllegalTransition()
    {
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDraft(1, DraftStatus.WorkInProgress));

        var result = await _handler.ApproveAsync(1, new ApproveDraftRequest { JournalistId = 7 });

        Assert.Equal(DraftActionOutcome.IllegalTransition, result.Outcome);
        _repository.Verify(r => r.ApproveAsync(It.IsAny<long>(), It.IsAny<DraftStatus>(), It.IsAny<long>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task ArchiveAsync_ConcurrentStatusChange_ReturnsConcurrentChange()
    {
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(MakeDraft(1, DraftStatus.WorkInProgress));
        _repository.Setup(r => r.UpdateStatusAsync(1, DraftStatus.WorkInProgress, DraftStatus.Archived))
            .ReturnsAsync((Draft?)null);

        var result = await _handler.ArchiveAsync(1);

        Assert.Equal(DraftActionOutcome.ConcurrentChange, result.Outcome);
    }

    [Fact]
    public async Task RejectAsync_NotFound_ReturnsNotFound()
    {
        _repository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Draft?)null);

        var result = await _handler.RejectAsync(1, new RejectDraftRequest());

        Assert.Equal(DraftActionOutcome.NotFound, result.Outcome);
    }
}
