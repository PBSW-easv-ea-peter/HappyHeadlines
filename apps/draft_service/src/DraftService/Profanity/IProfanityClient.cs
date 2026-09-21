namespace DraftService.Profanity;

public interface IProfanityClient
{
    Task<ProfanityCheckResult> CheckAsync(string text, CancellationToken cancellationToken = default);
}

// CircuitOpen is deliberately separate from IsProfane: a tripped circuit breaker means
// ProfanityService could not be asked at all, which is not the same thing as "the text
// was checked and found clean". Same reasoning as CommentService's ProfanityCheckResult.
public record ProfanityCheckResult(IReadOnlyList<string> BannedWords, bool CircuitOpen)
{
    public bool IsProfane => BannedWords.Count > 0;
}
