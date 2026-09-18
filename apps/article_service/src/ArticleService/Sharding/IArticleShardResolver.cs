namespace ArticleService.Sharding;

public interface IArticleShardResolver
{
    string GetConnectionString(string location);
}
