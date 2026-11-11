namespace NewsletterService.Services.ArticleService;

public class Article
{
    public required string Title { get; init; }
    public required string Byline { get; init; }
    public required string BreadText { get; init; }
    public required DateTimeOffset PublishDate { get; init; }
    public required int Location { get; init; }
    public required string SectionName { get; init; }
}