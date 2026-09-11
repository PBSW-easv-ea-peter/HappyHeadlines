using ProfanityService.Repositories;

namespace ProfanityService.Checking;

// Thin layer between Controller and Repository, matching the intended 3-tier architecture
// (see docs/comment_and_profanity_services_architecture.png). Gives a place to add
// richer profanity rules later (e.g. multi-word phrases) without touching the controller
// or the repository.
public class ProfanityChecker : IProfanityChecker
{
    private readonly IProfanityRepository _repository;

    public ProfanityChecker(IProfanityRepository repository)
    {
        _repository = repository;
    }

    public Task<bool> CheckAsync(string word) => _repository.IsProfaneAsync(word.Trim());
}
