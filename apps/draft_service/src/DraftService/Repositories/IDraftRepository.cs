using DraftService.Models;

namespace DraftService.Repositories;

public interface IDraftRepository
{
    Task<IEnumerable<Draft>> GetAllAsync(long? createdBy);
    Task<Draft?> GetByIdAsync(Guid id);
    Task<Draft> CreateAsync(CreateDraftRequest request);
    Task<Draft?> UpdateContentAsync(Guid id, EditDraftRequest request);
    Task<Draft?> SubmitForApprovalAsync(Guid id, DraftStatus expectedCurrentStatus, IReadOnlyList<string> flaggedWords);
    Task<Draft?> UpdateStatusAsync(Guid id, DraftStatus expectedCurrentStatus, DraftStatus newStatus);
    Task<Draft?> ApproveAsync(Guid id, DraftStatus expectedCurrentStatus, long approvedByJournalistId, string? note);
    Task<Draft?> RejectAsync(Guid id, DraftStatus expectedCurrentStatus, string? note);
}
