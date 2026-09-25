using DraftService.Handlers;
using DraftService.Models;
using Microsoft.AspNetCore.Mvc;

namespace DraftService.Controllers;

[ApiController]
[Route("api/drafts")]
public class DraftsController : ControllerBase
{
    private readonly IDraftHandler _handler;

    public DraftsController(IDraftHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Draft>>> GetAll([FromQuery] long? createdBy)
    {
        return Ok(await _handler.GetAllAsync(createdBy));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Draft>> GetById(Guid id)
    {
        var draft = await _handler.GetByIdAsync(id);
        return draft is null ? NotFound() : Ok(draft);
    }

    [HttpPost]
    public async Task<ActionResult<Draft>> Create(CreateDraftRequest request)
    {
        var result = await _handler.CreateAsync(request);
        return result.Outcome == DraftActionOutcome.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Draft!.Id }, result.Draft)
            : ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Draft>> Update(Guid id, EditDraftRequest request) =>
        ToActionResult(await _handler.UpdateContentAsync(id, request));

    [HttpPost("{id:guid}/submit-for-approval")]
    public async Task<ActionResult<Draft>> SubmitForApproval(Guid id, CancellationToken cancellationToken) =>
        ToActionResult(await _handler.SubmitForApprovalAsync(id, cancellationToken));

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<Draft>> Approve(Guid id, ApproveDraftRequest request) =>
        ToActionResult(await _handler.ApproveAsync(id, request));

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<Draft>> Reject(Guid id, RejectDraftRequest request) =>
        ToActionResult(await _handler.RejectAsync(id, request));

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<Draft>> Publish(Guid id) =>
        ToActionResult(await _handler.PublishAsync(id));

    [HttpPost("{id:guid}/archive")]
    public async Task<ActionResult<Draft>> Archive(Guid id) =>
        ToActionResult(await _handler.ArchiveAsync(id));

    [HttpPost("{id:guid}/reactivate")]
    public async Task<ActionResult<Draft>> Reactivate(Guid id) =>
        ToActionResult(await _handler.ReactivateAsync(id));

    private ActionResult<Draft> ToActionResult(DraftActionResult result) => result.Outcome switch
    {
        DraftActionOutcome.Success => Ok(result.Draft),
        DraftActionOutcome.NotFound => NotFound(),
        DraftActionOutcome.ValidationFailed => BadRequest(result.Message),
        DraftActionOutcome.IllegalTransition => Conflict(result.Message),
        DraftActionOutcome.ConcurrentChange => Conflict(result.Message),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };
}
