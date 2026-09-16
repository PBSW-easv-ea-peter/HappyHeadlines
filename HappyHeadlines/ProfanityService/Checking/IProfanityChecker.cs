namespace ProfanityService.Checking;

public interface IProfanityChecker
{
    Task<IReadOnlyList<string>> CheckAsync(string text);
}
