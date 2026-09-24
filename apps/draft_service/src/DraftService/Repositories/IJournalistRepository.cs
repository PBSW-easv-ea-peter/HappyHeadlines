using DraftService.Models;

namespace DraftService.Repositories;

public interface IJournalistRepository
{
    Task<IEnumerable<Journalist>> GetAllAsync();
    Task<bool> ExistsAsync(long id);
}
