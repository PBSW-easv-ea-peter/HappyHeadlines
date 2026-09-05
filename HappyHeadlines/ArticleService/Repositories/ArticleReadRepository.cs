using ArticleService.Models;
using ArticleService.Sharding;
using Dapper;
using Npgsql;

namespace ArticleService.Repositories;

public class ArticleReadRepository : IArticleReadRepository
{
    private readonly IArticleShardResolver _shardResolver;

    public ArticleReadRepository(IArticleShardResolver shardResolver)
    {
        _shardResolver = shardResolver;
    }

    public async Task<IEnumerable<Article>> GetAllAsync(string location)
    {
        await using var connection = new NpgsqlConnection(_shardResolver.GetConnectionString(location));
        const string sql = """
            select id, journalist_id as JournalistId, title, breadtext, created_date as CreatedDate,
                   publish_date as PublishDate, location, section_id as SectionId
            from articles
            """;
        return await connection.QueryAsync<Article>(sql);
    }

    public async Task<Article?> GetByIdAsync(string location, long id)
    {
        await using var connection = new NpgsqlConnection(_shardResolver.GetConnectionString(location));
        const string sql = """
            select id, journalist_id as JournalistId, title, breadtext, created_date as CreatedDate,
                   publish_date as PublishDate, location, section_id as SectionId
            from articles
            where id = @Id
            """;
        return await connection.QueryFirstOrDefaultAsync<Article>(sql, new { Id = id });
    }
}
