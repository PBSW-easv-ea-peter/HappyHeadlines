using PublishService.Models;

namespace PublishService.Services.External;

public interface IDraftService
{
    Task<Draft?> GetDraftAsync(Guid id);
}