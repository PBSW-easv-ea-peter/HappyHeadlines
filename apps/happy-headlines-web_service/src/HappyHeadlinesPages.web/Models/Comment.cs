namespace HappyHeadlinesPages.web.Models;

public class Comment
{
    public long Id { get; set; }
    public long ArticleId { get; set; }
    public string ArticleLocation { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedDate { get; set; }
    public CommentStatus Status { get; set; }
}

public enum CommentStatus
{
    Approved = 0,
    PendingProfanityCheck = 1,
    Rejected = 2
}
