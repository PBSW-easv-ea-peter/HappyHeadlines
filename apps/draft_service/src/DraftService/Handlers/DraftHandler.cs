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
    private readonly IJournalistRepository _journalistRepository;
    private readonly IProfanityClient _profanityClient;
    private readonly ILogger<DraftHandler> _logger;

    public DraftHandler(
        IDraftRepository repository,
        IJournalistRepository journalistRepository,
        IProfanityClient profanityClient,
        ILogger<DraftHandler> logger)
    {
        _repository = repository;
        _journalistRepository = journalistRepository;
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

        if (!Sections.IsValid(request.SectionId))
        {
            return DraftActionResult.ValidationFailed($"Unknown section {request.SectionId}.");
        }

        var creditedJournalistsError = await ValidateCreditedJournalistsAsync(request.CreditedJournalistIds);
        if (creditedJournalistsError is not null)
        {
            return DraftActionResult.ValidationFailed(creditedJournalistsError);
        }

        var draft = await _repository.CreateAsync(request);
        _logger.LogInformation("Draft {DraftId} created in {Location}", draft.Id, draft.Location);
        return DraftActionResult.Success(draft);
    }

    public async Task<DraftActionResult> UpdateContentAsync(Guid id, EditDraftRequest request)
    {
        if (!ValidLocations.Contains(request.Location))
        {
            return DraftActionResult.ValidationFailed($"Unknown location '{request.Location}'.");
        }

        if (!Sections.IsValid(request.SectionId))
        {
            return DraftActionResult.ValidationFailed($"Unknown section {request.SectionId}.");
        }

        var creditedJournalistsError = await ValidateCreditedJournalistsAsync(request.CreditedJournalistIds);
        if (creditedJournalistsError is not null)
        {
            return DraftActionResult.ValidationFailed(creditedJournalistsError);
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
        if (updated is null)
        {
            _logger.LogWarning("Draft {DraftId} was changed concurrently while in {Status}", id, draft.Status);
            return DraftActionResult.ConcurrentChange();
        }

        return DraftActionResult.Success(updated);
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

        // Known deviation from the architecture handout: the profanity check before publishing
        // belongs to PublisherService. It lives here until PublisherService is built, and nothing
        // new should be built on top of it (see docs/logging.md, open question 8).
        var profanityResult = await _profanityClient.CheckAsync(draft.Breadtext, cancellationToken);

        // Fault isolation, same principle as CommentService: if ProfanityService can't be
        // reached, submission still succeeds - the draft is marked as not checked, so the
        // editor is told to read it themselves instead of seeing "no issues found".
        if (profanityResult.Unavailable)
        {
            _logger.LogWarning("ProfanityService unavailable - submitting draft {DraftId} without a profanity check.", id);
        }

        var flaggedWords = profanityResult.Unavailable
            ? Array.Empty<string>()
            : profanityResult.BannedWords.ToArray();

        var updated = await _repository.SubmitForApprovalAsync(id, draft.Status, flaggedWords, profanityResult.Unavailable);
        return StatusChanged(id, draft.Status, updated);
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
        return StatusChanged(id, draft.Status, updated);
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
        return StatusChanged(id, draft.Status, updated);
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
        return StatusChanged(id, draft.Status, updated);
    }

    // Every status transition ends here, so it is logged in one place (docs/logging.md).
    // A null result means the optimistic-concurrency check in the repository failed.
    private DraftActionResult StatusChanged(Guid id, DraftStatus fromStatus, Draft? updated)
    {
        if (updated is null)
        {
            _logger.LogWarning("Draft {DraftId} was changed concurrently while in {Status}", id, fromStatus);
            return DraftActionResult.ConcurrentChange();
        }

        _logger.LogInformation(
            "Draft {DraftId} changed status from {FromStatus} to {ToStatus}",
            id, fromStatus, updated.Status);
        return DraftActionResult.Success(updated);
    }

    private async Task<string?> ValidateCreditedJournalistsAsync(IReadOnlyCollection<long> creditedJournalistIds)
    {
        var distinctIds = creditedJournalistIds.Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return null;
        }

        var existingIds = await _journalistRepository.FindExistingIdsAsync(distinctIds);
        var missingIds = distinctIds.Except(existingIds).ToList();

        return missingIds.Count == 0
            ? null
            : $"Unknown journalist id(s): {string.Join(", ", missingIds)}.";
    }

    private static string DescribeAction(DraftAction action) => action switch
    {
        DraftAction.Publish => "publish",
        DraftAction.Archive => "archive",
        DraftAction.Reactivate => "reactivate",
        _ => action.ToString()
    };
}
