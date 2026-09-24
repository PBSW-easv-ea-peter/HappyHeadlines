using DraftService.Models;
using DraftService.Profanity;
using DraftService.Repositories;
using DraftService.Workflow;

namespace DraftService.Handlers;

// Orchestrates draft business rules and persistence, kept out of the controller so
// DraftsController only deals with HTTP concerns (route binding, mapping outcomes to status
// codes) - the same separation CommentService uses between CommentsController and CommentHandler.
public class DraftHandler : IDraftHandler
{
    private static readonly HashSet<string> ValidLocations =
        new(StringComparer.OrdinalIgnoreCase) { "EU", "NA", "SA", "AU", "AS", "AN", "AF", "GO" };

    private readonly IDraftRepository _repository;
    private readonly IProfanityClient _profanityClient;
    private readonly ILogger<DraftHandler> _logger;

    public DraftHandler(IDraftRepository repository, IProfanityClient profanityClient, ILogger<DraftHandler> logger)
    {
        _repository = repository;
        _profanityClient = profanityClient;
        _logger = logger;
    }

    public Task<IEnumerable<Draft>> GetAllAsync(long? createdBy) => _repository.GetAllAsync(createdBy);

    public Task<Draft?> GetByIdAsync(Guid id) => _repository.GetByIdAsync(id);

    public async Task<DraftActionResult> CreateAsync(CreateDraftRequest request)
    {
        if (!ValidLocations.Contains(request.Location))
        {
            return DraftActionResult.ValidationFailed($"Unknown location '{request.Location}'.");
        }

        var draft = await _repository.CreateAsync(request);
        return DraftActionResult.Success(draft);
    }

    public async Task<DraftActionResult> UpdateContentAsync(Guid id, EditDraftRequest request)
    {
        if (!ValidLocations.Contains(request.Location))
        {
            return DraftActionResult.ValidationFailed($"Unknown location '{request.Location}'.");
        }

        var draft = await _repository.GetByIdAsync(id);
        if (draft is null)
        {
            return DraftActionResult.NotFound();
        }

        if (!DraftStatusTransitions.CanEditContent(draft.Status))
        {
            return DraftActionResult.IllegalTransition($"Cannot edit a draft in {draft.Status} status.");
        }

        var updated = await _repository.UpdateContentAsync(id, request);
        return updated is null
            ? DraftActionResult.ConcurrentChange()
            : DraftActionResult.Success(updated);
    }

    public async Task<DraftActionResult> SubmitForApprovalAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var draft = await _repository.GetByIdAsync(id);
        if (draft is null)
        {
            return DraftActionResult.NotFound();
        }

        if (!DraftStatusTransitions.TryGetResultStatus(DraftAction.SubmitForApproval, draft.Status, out _))
        {
            return DraftActionResult.IllegalTransition($"Cannot submit for approval a draft in {draft.Status} status.");
        }

        var profanityResult = await _profanityClient.CheckAsync(draft.Breadtext, cancellationToken);

        // Fault isolation, same principle as CommentService: if ProfanityService can't be
        // reached, submission still succeeds - the editor just won't get a pre-flagged word
        // list for this pass and has to read the draft themselves.
        if (profanityResult.CircuitOpen)
        {
            _logger.LogWarning("ProfanityService unavailable - submitting draft {DraftId} without flagged words.", id);
        }

        var flaggedWords = profanityResult.CircuitOpen
            ? Array.Empty<string>()
            : profanityResult.BannedWords.ToArray();

        var updated = await _repository.SubmitForApprovalAsync(id, draft.Status, flaggedWords);
        return updated is null
            ? DraftActionResult.ConcurrentChange()
            : DraftActionResult.Success(updated);
    }

    public async Task<DraftActionResult> ApproveAsync(Guid id, ApproveDraftRequest request)
    {
        var draft = await _repository.GetByIdAsync(id);
        if (draft is null)
        {
            return DraftActionResult.NotFound();
        }

        if (!DraftStatusTransitions.TryGetResultStatus(DraftAction.Approve, draft.Status, out _))
        {
            return DraftActionResult.IllegalTransition($"Cannot approve a draft in {draft.Status} status.");
        }

        var updated = await _repository.ApproveAsync(id, draft.Status, request.JournalistId, request.Note);
        return updated is null
            ? DraftActionResult.ConcurrentChange()
            : DraftActionResult.Success(updated);
    }

    public async Task<DraftActionResult> RejectAsync(Guid id, RejectDraftRequest request)
    {
        var draft = await _repository.GetByIdAsync(id);
        if (draft is null)
        {
            return DraftActionResult.NotFound();
        }

        if (!DraftStatusTransitions.TryGetResultStatus(DraftAction.Reject, draft.Status, out _))
        {
            return DraftActionResult.IllegalTransition($"Cannot reject a draft in {draft.Status} status.");
        }

        var updated = await _repository.RejectAsync(id, draft.Status, request.Note);
        return updated is null
            ? DraftActionResult.ConcurrentChange()
            : DraftActionResult.Success(updated);
    }

    public Task<DraftActionResult> PublishAsync(Guid id) => TransitionAsync(id, DraftAction.Publish);

    public Task<DraftActionResult> ArchiveAsync(Guid id) => TransitionAsync(id, DraftAction.Archive);

    public Task<DraftActionResult> ReactivateAsync(Guid id) => TransitionAsync(id, DraftAction.Reactivate);

    private async Task<DraftActionResult> TransitionAsync(Guid id, DraftAction action)
    {
        var draft = await _repository.GetByIdAsync(id);
        if (draft is null)
        {
            return DraftActionResult.NotFound();
        }

        if (!DraftStatusTransitions.TryGetResultStatus(action, draft.Status, out var newStatus))
        {
            return DraftActionResult.IllegalTransition($"Cannot {DescribeAction(action)} a draft in {draft.Status} status.");
        }

        var updated = await _repository.UpdateStatusAsync(id, draft.Status, newStatus);
        return updated is null
            ? DraftActionResult.ConcurrentChange()
            : DraftActionResult.Success(updated);
    }

    private static string DescribeAction(DraftAction action) => action switch
    {
        DraftAction.Publish => "publish",
        DraftAction.Archive => "archive",
        DraftAction.Reactivate => "reactivate",
        _ => action.ToString()
    };
}
