using ArticleService.Models;
using ArticleService.Sharding;
using Dapper;
using Npgsql;

namespace ArticleService.Repositories;

public class ArticleWriteRepository : IArticleWriteRepository
{
    private readonly IArticleShardResolver _shardResolver;

    public ArticleWriteRepository(IArticleShardResolver shardResolver)
    {
        _shardResolver = shardResolver;
    }

    public async Task<Article> CreateAsync(string location, UpsertArticleRequest request)
    {
        await using var connection = new NpgsqlConnection(_shardResolver.GetConnectionString(location));
        const string sql = """
            insert into articles (journalist_id, title, breadtext, publish_date, location, section_id)
            values (@JournalistId, @Title, @Breadtext, @PublishDate, @Location, @SectionId)
            returning id, journalist_id as JournalistId, title, breadtext, created_date as CreatedDate,
                      publish_date as PublishDate, location, section_id as SectionId
            """;

        return await connection.QuerySingleAsync<Article>(sql, new
        {
            request.JournalistId,
            request.Title,
            request.Breadtext,
            request.PublishDate,
            Location = location.ToUpperInvariant(),
            request.SectionId
        });
    }

    public async Task<bool> UpdateAsync(string location, long id, UpsertArticleRequest request)
    {
        await using var connection = new NpgsqlConnection(_shardResolver.GetConnectionString(location));
        const string sql = """
            update articles
            set journalist_id = @JournalistId,
                title = @Title,
                breadtext = @Breadtext,
                publish_date = @PublishDate,
                section_id = @SectionId
            where id = @Id
            """;

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            request.JournalistId,
            request.Title,
            request.Breadtext,
            request.PublishDate,
            request.SectionId,
            Id = id
        });

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string location, long id)
    {
        await using var connection = new NpgsqlConnection(_shardResolver.GetConnectionString(location));
        const string sql = "delete from articles where id = @Id";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }
}
