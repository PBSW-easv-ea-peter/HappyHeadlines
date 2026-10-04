using PublishService.Shared.Models;

namespace PublishService.Shared.External;

public interface IHttpDraftClient
{
    Task<DraftDTO?> GetAsync(Guid id);
}