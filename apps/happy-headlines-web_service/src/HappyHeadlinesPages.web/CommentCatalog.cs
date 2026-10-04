using HappyHeadlinesPages.web.Models;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace HappyHeadlinesPages.web;

// What happened to a posted comment, as far as the reader needs to know.
public enum CommentPostOutcome
{
    Published,   // passed the profanity check and is visible now
    Rejected,    // saved, but failed the profanity check - never shown
    NotVerified, // ProfanityService unavailable (422) - saved as pending and hidden
    Invalid,     // 400 from CommentService, e.g. empty name/text
    Failed       // network error or unexpected status
}

public sealed record CommentPostResult(CommentPostOutcome Outcome, Comment? Comment = null, string? Error = null);

public static class CommentCatalog
{
    // Mirrors the comments table (database/init/comment/comment_baseline.sql).
    // CommentService doesn't validate lengths itself, so callers must.
    public const int MaxAuthorNameLength = 25;
    public const int MaxTextLength = 500;

    private const string CommentServiceBaseUrl = "http://localhost:8082";

    // CommentService only returns approved comments from this endpoint, so the
    // result is safe to show to readers as-is.
    public static async Task<List<Comment>> GetApprovedAsync(HttpClient http, ILogger logger, string region, long articleId)
    {
        var regionCode = region.ToUpper();

        try
        {
            var comments = await http.GetFromJsonAsync<List<Comment>>(
                $"{CommentServiceBaseUrl}/api/comments/{regionCode}/{articleId}");

            return comments ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load comments for article {ArticleId} in region {Region}", articleId, regionCode);
            return [];
        }
    }

    // The profanity check runs synchronously inside the POST, so the outcome is
    // known as soon as the call returns (see docs/comment_and_profanity_service.md).
    public static async Task<CommentPostResult> PostAsync(
        HttpClient http, ILogger logger, string region, long articleId, string authorName, string text)
    {
        var regionCode = region.ToUpper();

        try
        {
            var response = await http.PostAsJsonAsync(
                $"{CommentServiceBaseUrl}/api/comments/{regionCode}/{articleId}",
                new { AuthorName = authorName, Text = text });

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
                return new CommentPostResult(CommentPostOutcome.NotVerified);

            if (response.StatusCode == HttpStatusCode.BadRequest)
                return new CommentPostResult(CommentPostOutcome.Invalid, Error: await response.Content.ReadAsStringAsync());

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Posting a comment on article {ArticleId} in {Region} returned {StatusCode}",
                    articleId, regionCode, (int)response.StatusCode);
                return new CommentPostResult(CommentPostOutcome.Failed);
            }

            var comment = await response.Content.ReadFromJsonAsync<Comment>();

            return comment?.Status switch
            {
                CommentStatus.Approved => new CommentPostResult(CommentPostOutcome.Published, comment),
                CommentStatus.Rejected => new CommentPostResult(CommentPostOutcome.Rejected, comment),
                _ => new CommentPostResult(CommentPostOutcome.NotVerified, comment)
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to post comment on article {ArticleId} in region {Region}", articleId, regionCode);
            return new CommentPostResult(CommentPostOutcome.Failed);
        }
    }
}
