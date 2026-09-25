using DraftService.Controllers;
using DraftService.Handlers;
using DraftService.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DraftService.Tests.Controllers;

public class DraftsControllerTests
{
    private static readonly Guid DraftId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly Mock<IDraftHandler> _handler = new();
    private readonly DraftsController _controller;

    public DraftsControllerTests()
    {
        _controller = new DraftsController(_handler.Object);
    }

    private static Draft MakeDraft(Guid id, DraftStatus status) => new()
    {
        Id = id,
        Title = "Title",
        Breadtext = "Body",
        Location = "EU",
        SectionId = 1,
        CreatedByJournalistId = 1,
        LastEditedByJournalistId = 1,
        Status = status
    };

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        _handler.Setup(h => h.GetByIdAsync(DraftId)).ReturnsAsync((Draft?)null);

        var result = await _controller.GetById(DraftId);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetById_KnownId_ReturnsDraft()
    {
        var draft = MakeDraft(DraftId, DraftStatus.WorkInProgress);
        _handler.Setup(h => h.GetByIdAsync(DraftId)).ReturnsAsync(draft);

        var result = await _controller.GetById(DraftId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(draft, ok.Value);
    }

    [Fact]
    public async Task Create_ValidationFailed_ReturnsBadRequest()
    {
        var request = new CreateDraftRequest { JournalistId = 1, Title = "T", Breadtext = "B", Location = "XX", SectionId = 1 };
        _handler.Setup(h => h.CreateAsync(request)).ReturnsAsync(DraftActionResult.ValidationFailed("Unknown location 'XX'."));

        var result = await _controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_Success_ReturnsCreatedAtAction()
    {
        var request = new CreateDraftRequest { JournalistId = 1, Title = "T", Breadtext = "B", Location = "EU", SectionId = 1 };
        var draft = MakeDraft(DraftId, DraftStatus.WorkInProgress);
        _handler.Setup(h => h.CreateAsync(request)).ReturnsAsync(DraftActionResult.Success(draft));

        var result = await _controller.Create(request);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Same(draft, created.Value);
    }

    [Fact]
    public async Task Update_NotFound_ReturnsNotFound()
    {
        var request = new EditDraftRequest { JournalistId = 1, Title = "T", Breadtext = "B", Location = "EU", SectionId = 1 };
        _handler.Setup(h => h.UpdateContentAsync(DraftId, request)).ReturnsAsync(DraftActionResult.NotFound());

        var result = await _controller.Update(DraftId, request);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Update_IllegalTransition_ReturnsConflict()
    {
        var request = new EditDraftRequest { JournalistId = 1, Title = "T", Breadtext = "B", Location = "EU", SectionId = 1 };
        _handler.Setup(h => h.UpdateContentAsync(DraftId, request))
            .ReturnsAsync(DraftActionResult.IllegalTransition("Cannot edit a draft in PendingApproval status."));

        var result = await _controller.Update(DraftId, request);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_Success_ReturnsOk()
    {
        var request = new EditDraftRequest { JournalistId = 1, Title = "New", Breadtext = "B", Location = "EU", SectionId = 1 };
        var updated = MakeDraft(DraftId, DraftStatus.WorkInProgress);
        _handler.Setup(h => h.UpdateContentAsync(DraftId, request)).ReturnsAsync(DraftActionResult.Success(updated));

        var result = await _controller.Update(DraftId, request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(updated, ok.Value);
    }

    [Fact]
    public async Task SubmitForApproval_Success_ReturnsOk()
    {
        var updated = MakeDraft(DraftId, DraftStatus.PendingApproval);
        _handler.Setup(h => h.SubmitForApprovalAsync(DraftId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DraftActionResult.Success(updated));

        var result = await _controller.SubmitForApproval(DraftId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(updated, ok.Value);
    }

    [Fact]
    public async Task Approve_IllegalTransition_ReturnsConflict()
    {
        _handler.Setup(h => h.ApproveAsync(DraftId, It.IsAny<ApproveDraftRequest>()))
            .ReturnsAsync(DraftActionResult.IllegalTransition("Cannot approve a draft in WorkInProgress status."));

        var result = await _controller.Approve(DraftId, new ApproveDraftRequest { JournalistId = 7 });

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Archive_ConcurrentChange_ReturnsConflict()
    {
        _handler.Setup(h => h.ArchiveAsync(DraftId)).ReturnsAsync(DraftActionResult.ConcurrentChange());

        var result = await _controller.Archive(DraftId);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }
}
