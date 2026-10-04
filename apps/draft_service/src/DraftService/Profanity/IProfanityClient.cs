namespace DraftService.Profanity;

public interface IProfanityClient
{
    Task<ProfanityCheckResult> CheckAsync(string text, CancellationToken cancellationToken = default);
}

// Unavailable is deliberately separate from IsProfane: it means ProfanityService gave no
// answer (circuit open, unreachable, timed out, error response), which is not the same thing
// as "the text was checked and found clean". Same reasoning as CommentService's ProfanityCheckResult.
public record ProfanityCheckResult(IReadOnlyList<string> BannedWords, bool Unavailable)
{
    public bool IsProfane => BannedWords.Count > 0;

    public static ProfanityCheckResult NotChecked { get; } = new([], Unavailable: true);
}
