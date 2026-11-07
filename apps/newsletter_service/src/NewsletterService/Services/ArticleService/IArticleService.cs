
namespace NewsletterService.Services.ArticleService;

public interface IArticleService
{
    public Task<IReadOnlyCollection<Article>> GetArticlesSinceAsync(DateTimeOffset since);
    public Task<IReadOnlyCollection<Section>> GetSectionsAsync();
}

public class FakeArticleService : IArticleService
{
    private static List<Article> Articles => FakeArticles.Data;
    
    public Task<IReadOnlyCollection<Article>> GetArticlesSinceAsync(DateTimeOffset since)
    {
        IReadOnlyCollection<Article> result =
        [
            .. Articles
                .Where(a => a.PublishDate > since)
                .OrderByDescending(a => a.PublishDate)
        ];

        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<Section>> GetSectionsAsync()
    {
        return Task.FromResult<IReadOnlyCollection<Section>>(FakeSections.Data);
    }
}