using PublishService.Models;

namespace PublishService.Services.External;

public interface IDraftService
{
    Task<DraftDTO?> GetDraftAsync(Guid id);
}