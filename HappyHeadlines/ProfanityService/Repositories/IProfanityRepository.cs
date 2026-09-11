namespace ProfanityService.Repositories;

public interface IProfanityRepository
{
    Task<bool> IsProfaneAsync(string word);
}
