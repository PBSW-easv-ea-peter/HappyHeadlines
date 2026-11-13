using ArticleService.Models;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ArticleService.Sharding;

public class ArticleShardResolver : IArticleShardResolver
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ArticleShardResolver> _logger;
    private readonly IOptionsMonitor<LocationConfig> _locations;

    public ArticleShardResolver(
        IConfiguration configuration,
        ILogger<ArticleShardResolver> logger,
        IOptionsMonitor<LocationConfig> locations)
    {
        _configuration = configuration;
        _logger = logger;
        _locations = locations;
    }

    public string GetConnectionString(string location)
    {
        string? connectionString =
            _configuration[$"ArticleShards:{location.ToUpperInvariant()}"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogError("There where no connection string found for locations: {Loc}", location);
            return string.Empty;
        }

        string? dbPassword = _configuration["POSTGRES_DB_PWD"];

        var connectionStringBuilder =
            new NpgsqlConnectionStringBuilder(connectionString)
            {
                Password = dbPassword
            };

        return connectionStringBuilder.ConnectionString;
    }
}
