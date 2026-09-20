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

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Draft>> GetById(long id)
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

    [HttpPut("{id:long}")]
    public async Task<ActionResult<Draft>> Update(long id, EditDraftRequest request) =>
        ToActionResult(await _handler.UpdateContentAsync(id, request));

    [HttpPost("{id:long}/submit-for-approval")]
    public async Task<ActionResult<Draft>> SubmitForApproval(long id, CancellationToken cancellationToken) =>
        ToActionResult(await _handler.SubmitForApprovalAsync(id, cancellationToken));

    [HttpPost("{id:long}/approve")]
    public async Task<ActionResult<Draft>> Approve(long id, ApproveDraftRequest request) =>
        ToActionResult(await _handler.ApproveAsync(id, request));

    [HttpPost("{id:long}/reject")]
    public async Task<ActionResult<Draft>> Reject(long id, RejectDraftRequest request) =>
        ToActionResult(await _handler.RejectAsync(id, request));

    [HttpPost("{id:long}/publish")]
    public async Task<ActionResult<Draft>> Publish(long id) =>
        ToActionResult(await _handler.PublishAsync(id));

    [HttpPost("{id:long}/archive")]
    public async Task<ActionResult<Draft>> Archive(long id) =>
        ToActionResult(await _handler.ArchiveAsync(id));

    [HttpPost("{id:long}/reactivate")]
    public async Task<ActionResult<Draft>> Reactivate(long id) =>
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
