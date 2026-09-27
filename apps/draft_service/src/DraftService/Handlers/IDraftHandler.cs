using DraftService.Models;

namespace DraftService.Handlers;

public interface IDraftHandler
{
    Task<IEnumerable<Draft>> GetAllAsync(long? createdBy);
    Task<Draft?> GetByIdAsync(long id);
    Task<DraftActionResult> CreateAsync(CreateDraftRequest request);
    Task<DraftActionResult> UpdateContentAsync(long id, EditDraftRequest request);
    Task<DraftActionResult> SubmitForApprovalAsync(long id, CancellationToken cancellationToken = default);
    Task<DraftActionResult> ApproveAsync(long id, ApproveDraftRequest request);
    Task<DraftActionResult> RejectAsync(long id, RejectDraftRequest request);
    Task<DraftActionResult> PublishAsync(long id);
    Task<DraftActionResult> ArchiveAsync(long id);
    Task<DraftActionResult> ReactivateAsync(long id);
}
