namespace ProfanityService.Checking;

public interface IProfanityChecker
{
    Task<bool> CheckAsync(string word);
}
