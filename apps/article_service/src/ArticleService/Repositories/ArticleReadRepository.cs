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
        await using var connection = new NpgsqlConnection(
            _shardResolver.GetConnectionString(location));

        const string sql = """
            select
                a.id,
                a.byline as Byline,
                a.title,
                a.breadtext,
                a.created_date as CreatedDate,
                a.publish_date as PublishDate,
                a.location,
                s.name as SectionName
            from articles a
            inner join sections s on s.id = a.section_id
            """;

        return await connection.QueryAsync<Article>(sql);
    }

    public async Task<Article?> GetByIdAsync(string location, long id)
    {
        await using var connection = new NpgsqlConnection(
            _shardResolver.GetConnectionString(location));

        const string sql = """
            select
                a.id,
                a.byline as Byline,
                a.title,
                a.breadtext,
                a.created_date as CreatedDate,
                a.publish_date as PublishDate,
                a.location,
                s.name as SectionName
            from articles a
            inner join sections s on s.id = a.section_id
            where a.id = @Id
            """;

        return await connection.QueryFirstOrDefaultAsync<Article>(
            sql,
            new { Id = id });
    }
}
