using DraftService.Models;

namespace DraftService.Handlers;

public interface IDraftHandler
{
    Task<IEnumerable<Draft>> GetAllAsync(long? createdBy);
    Task<Draft?> GetByIdAsync(Guid id);
    Task<DraftActionResult> CreateAsync(CreateDraftRequest request);
    Task<DraftActionResult> UpdateContentAsync(Guid id, EditDraftRequest request);
    Task<DraftActionResult> SubmitForApprovalAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DraftActionResult> ApproveAsync(Guid id, ApproveDraftRequest request);
    Task<DraftActionResult> RejectAsync(Guid id, RejectDraftRequest request);
    Task<DraftActionResult> PublishAsync(Guid id);
    Task<DraftActionResult> ArchiveAsync(Guid id);
    Task<DraftActionResult> ReactivateAsync(Guid id);
}
