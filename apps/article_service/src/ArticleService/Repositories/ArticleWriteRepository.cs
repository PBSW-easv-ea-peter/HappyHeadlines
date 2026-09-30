using ArticleService.Models;
using ArticleService.Sharding;
using Dapper;
using Messaging.Events;
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
            insert into articles (byline, title, breadtext, publish_date, location, section_id)
            values (@Byline, @Title, @Breadtext, @PublishDate, @Location, @SectionId)
            returning id, byline as Byline, title, breadtext, created_date as CreatedDate,
                      publish_date as PublishDate, location, section_id as SectionId
            """;

        return await connection.QuerySingleAsync<Article>(sql, new
        {
            request.Byline,
            request.Title,
            request.Breadtext,
            request.PublishDate,
            Location = location.ToUpperInvariant(),
            request.SectionId
        });
    }

    public async Task<bool> CreatePublishedAsync(PublishedArticleEvent published)
    {
        await using var connection = new NpgsqlConnection(_shardResolver.GetConnectionString(published.Location));

        var sectionId = await connection.QuerySingleOrDefaultAsync<long?>(
            "select id from sections where name = @SectionName",
            new { published.SectionName })
            ?? throw new ArgumentException($"Unknown section '{published.SectionName}'.");

        const string sql = """
            insert into articles (draft_id, byline, title, breadtext, created_date, publish_date, location, section_id)
            values (@DraftId, @Byline, @Title, @Breadtext, @CreatedDate, @PublishDate, @Location, @SectionId)
            on conflict (draft_id) do nothing
            """;

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            published.DraftId,
            Byline = published.JournalistName,
            published.Title,
            Breadtext = published.BreadText,
            CreatedDate = new DateTimeOffset(DateTime.SpecifyKind(published.CreatedDate, DateTimeKind.Utc)),
            PublishDate = new DateTimeOffset(DateTime.SpecifyKind(published.PublishDate, DateTimeKind.Utc)),
            Location = published.Location.ToUpperInvariant(),
            SectionId = sectionId
        });

        return rowsAffected > 0;
    }

    public async Task<bool> UpdateAsync(string location, long id, UpsertArticleRequest request)
    {
        await using var connection = new NpgsqlConnection(_shardResolver.GetConnectionString(location));
        const string sql = """
            update articles
            set byline = @Byline,
                title = @Title,
                breadtext = @Breadtext,
                publish_date = @PublishDate,
                section_id = @SectionId
            where id = @Id
            """;

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            request.Byline,
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
