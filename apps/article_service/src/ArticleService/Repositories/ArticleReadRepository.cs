using ArticleService.Models;
using ArticleService.Sharding;
using Dapper;
using Npgsql;

namespace ArticleService.Repositories;

public class ArticleReadRepository : IArticleReadRepository
{
    private readonly IArticleShardResolver _shardResolver;
    private readonly ILogger<ArticleReadRepository> _logger;

    public ArticleReadRepository(
            IArticleShardResolver shardResolver,
            ILogger<ArticleReadRepository> logger)
    {
        _shardResolver = shardResolver;
        _logger = logger;
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

    // Exactly what a front page shows, in one query: the newest Top, the newest PerSection in
    // every section, and the newest Top in Section (if given) - only those rows leave the
    // database. With an excerpt length the body text is cut in the database too; +1 character
    // lets the client see that the text goes on, so its own Excerpt() still adds the ellipsis.
    // Ordered like the client orders (PublishDate, falling back to CreatedDate).
    public async Task<IEnumerable<Article>> GetFrontPageAsync(string location, FrontPageQuery query)
    {
        await using var connection = new NpgsqlConnection(
            _shardResolver.GetConnectionString(location));

        const string sql = """
            with ranked as (
                select
                    a.id, a.byline, a.title, a.breadtext, a.created_date, a.publish_date, a.location,
                    s.name as section_name,
                    coalesce(a.publish_date, a.created_date) as shown_at,
                    row_number() over (
                        order by coalesce(a.publish_date, a.created_date) desc, a.id) as rn_all,
                    row_number() over (
                        partition by a.section_id
                        order by coalesce(a.publish_date, a.created_date) desc, a.id) as rn_section
                from articles a
                inner join sections s on s.id = a.section_id
            ),
            picked as (
                select
                    *,
                    case when (case when cast(@Section as text) is null then rn_all = 1
                                    else lower(section_name) = lower(cast(@Section as text)) and rn_section = 1
                               end)
                         then coalesce(cast(@HeroExcerpt as int), cast(@Excerpt as int))
                         else cast(@Excerpt as int)
                    end as excerpt_length
                from ranked
                where rn_all <= @Top
                   or rn_section <= @PerSection
                   or (lower(section_name) = lower(cast(@Section as text)) and rn_section <= @Top)
            )
            select
                id,
                byline as Byline,
                title,
                case when excerpt_length is null then breadtext
                     when excerpt_length = 0 then ''
                     else left(breadtext, excerpt_length + 1)
                end as Breadtext,
                created_date as CreatedDate,
                publish_date as PublishDate,
                location,
                section_name as SectionName
            from picked
            order by shown_at desc, id
            """;

        return await connection.QueryAsync<Article>(sql, new
        {
            query.Top,
            query.PerSection,
            query.Section,
            query.Excerpt,
            query.HeroExcerpt,
        });
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
