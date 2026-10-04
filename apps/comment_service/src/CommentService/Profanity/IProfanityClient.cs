namespace CommentService.Profanity;

public interface IProfanityClient
{
    Task<ProfanityCheckResult> CheckAsync(string text, CancellationToken cancellationToken = default);
}

// Unavailable is deliberately separate from IsProfane: it means ProfanityService gave no
// answer (circuit open, unreachable, timed out, error response), which is not the same thing
// as "the text was checked and found clean". Collapsing the two would silently let profanity
// through whenever ProfanityService is down - see docs/comment_and_profanity_service.md.
public record ProfanityCheckResult(IReadOnlyList<string> BannedWords, bool Unavailable)
{
    public bool IsProfane => BannedWords.Count > 0;

    public static ProfanityCheckResult NotChecked { get; } = new([], Unavailable: true);
}
