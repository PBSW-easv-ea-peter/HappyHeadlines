using CommentService.Models;
using Dapper;
using Npgsql;

namespace CommentService.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly string _connectionString;

    public CommentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("CommentDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:CommentDatabase is not configured.");
    }

    public async Task<IEnumerable<CommentEntity>> GetApprovedByArticleIdAsync(string articleLocation, long articleId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        const string sql = """
            select id, article_id as ArticleId, article_location as ArticleLocation, author_name as AuthorName, text, created_date as CreatedDate, status
            from comments
            where article_id = @ArticleId and article_location = @ArticleLocation and status = @Status
            order by created_date
            """;
        return await connection.QueryAsync<CommentEntity>(sql, new { ArticleId = articleId, ArticleLocation = articleLocation, Status = (short)CommentStatus.Approved });
    }

    public async Task<CommentEntity> CreateAsync(string articleLocation, long articleId, PostCommentRequest request, CommentStatus status)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        const string sql = """
            insert into comments (article_id, article_location, author_name, text, status)
            values (@ArticleId, @ArticleLocation, @AuthorName, @Text, @Status)
            returning id, article_id as ArticleId, article_location as ArticleLocation, author_name as AuthorName, text, created_date as CreatedDate, status
            """;

        return await connection.QuerySingleAsync<CommentEntity>(sql, new
        {
            ArticleId = articleId,
            ArticleLocation = articleLocation,
            request.AuthorName,
            request.Text,
            Status = (short)status
        });
    }
}
