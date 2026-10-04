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

    // Articles published between `since` and now. Articles without a publish date or with
    // one in the future aren't published yet, so they're left out.
    public async Task<IEnumerable<Article>> GetPublishedSinceAsync(string location, DateTimeOffset since)
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
            where a.publish_date >= @Since
              and a.publish_date <= now()
            order by a.publish_date desc
            """;

        return await connection.QueryAsync<Article>(
            sql,
            new { Since = since });
    }

    public async Task<Article?> GetByIdAsync(string location, Guid id)
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
