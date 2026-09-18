namespace CommentService.Models;

// Numeric values are persisted directly (see database/init/comment/comment_baseline.sql)
// - do not reorder the members without also updating the check_comment_status constraint.
public enum CommentStatus
{
    Approved = 0,
    PendingProfanityCheck = 1,
    Rejected = 2
}

public class CommentEntity
{
    public long Id { get; set; }
    public long ArticleId { get; set; }
    public string ArticleLocation { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedDate { get; set; }
    public CommentStatus Status { get; set; }
}
