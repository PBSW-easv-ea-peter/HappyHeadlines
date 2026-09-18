namespace ProfanityService.Repositories;

public interface IProfanityRepository
{
    Task<IReadOnlyList<string>> FindBannedWordsAsync(IEnumerable<string> words);
}
