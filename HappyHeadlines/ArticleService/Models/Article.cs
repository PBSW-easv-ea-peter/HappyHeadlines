namespace ArticleService.Models;

public class Article
{
    public long Id { get; set; }
    public long JournalistId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Breadtext { get; set; } = string.Empty;
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset? PublishDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public long SectionId { get; set; }
}

public class UpsertArticleRequest
{
    public long JournalistId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Breadtext { get; set; } = string.Empty;
    public DateTimeOffset? PublishDate { get; set; }
    public long SectionId { get; set; }
}
