namespace CommentService.Profanity;

public interface IProfanityClient
{
    Task<ProfanityCheckResult> CheckAsync(string text, CancellationToken cancellationToken = default);
}

// CircuitOpen is deliberately separate from IsProfane: a tripped circuit breaker means
// ProfanityService could not be asked at all, which is not the same thing as "the text
// was checked and found clean". Collapsing the two would silently let profanity through
// whenever ProfanityService is down - see docs/comment_and_profanity_services.md.
public record ProfanityCheckResult(IReadOnlyList<string> BannedWords, bool CircuitOpen)
{
    public bool IsProfane => BannedWords.Count > 0;
}
