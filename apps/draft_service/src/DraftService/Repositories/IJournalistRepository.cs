using DraftService.Models;

namespace DraftService.Repositories;

public interface IJournalistRepository
{
    Task<IEnumerable<Journalist>> GetAllAsync();
    Task<IReadOnlyCollection<long>> FindExistingIdsAsync(IReadOnlyCollection<long> ids);
}
