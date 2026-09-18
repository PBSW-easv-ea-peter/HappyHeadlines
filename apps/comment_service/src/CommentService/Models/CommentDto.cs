namespace CommentService.Models;

public class CommentDto
{
    public long Id { get; set; }
    public long ArticleId { get; set; }
    public string ArticleLocation { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedDate { get; set; }
    public CommentStatus Status { get; set; }

    public static CommentDto FromEntity(CommentEntity entity) => new()
    {
        Id = entity.Id,
        ArticleId = entity.ArticleId,
        ArticleLocation = entity.ArticleLocation,
        AuthorName = entity.AuthorName,
        Text = entity.Text,
        CreatedDate = entity.CreatedDate,
        Status = entity.Status
    };
}

public class PostCommentRequest
{
    public string AuthorName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
