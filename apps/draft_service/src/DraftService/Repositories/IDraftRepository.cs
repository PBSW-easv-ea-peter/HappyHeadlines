using DraftService.Models;

namespace DraftService.Repositories;

public interface IDraftRepository
{
    Task<IEnumerable<Draft>> GetAllAsync(long? createdBy);
    Task<Draft?> GetByIdAsync(long id);
    Task<Draft> CreateAsync(CreateDraftRequest request);
    Task<Draft?> UpdateContentAsync(long id, EditDraftRequest request);
    Task<Draft?> SubmitForApprovalAsync(long id, DraftStatus expectedCurrentStatus, IReadOnlyList<string> flaggedWords);
    Task<Draft?> UpdateStatusAsync(long id, DraftStatus expectedCurrentStatus, DraftStatus newStatus);
    Task<Draft?> ApproveAsync(long id, DraftStatus expectedCurrentStatus, long approvedByJournalistId, string? note);
    Task<Draft?> RejectAsync(long id, DraftStatus expectedCurrentStatus, string? note);
}
