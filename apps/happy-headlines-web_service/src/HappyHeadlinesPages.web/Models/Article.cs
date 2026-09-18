namespace HappyHeadlinesPages.web.Models;

public class Article : ArticleDTO
{
    public IEnumerable<Comment> Comments { get; set; } = [];
}

public class ArticleDTO
{
    public long Id { get; set; }
    public string JournalistName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset CreatedDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string BreadText { get; set; } = string.Empty;
}
