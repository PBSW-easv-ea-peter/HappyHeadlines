namespace ArticleService.Sharding;

public class ArticleShardResolver : IArticleShardResolver
{
    private readonly IConfiguration _configuration;

    public ArticleShardResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetConnectionString(string location)
    {
        var connectionString = _configuration[$"ArticleShards:{location.ToUpperInvariant()}"];

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException(
                $"Unknown location '{location}'. Valid values: EU, NA, SA, AU, AS, AN, AF, GO.");
        }

        return connectionString;
    }
}
