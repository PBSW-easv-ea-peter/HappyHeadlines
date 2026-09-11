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

    public async Task<IEnumerable<Comment>> GetApprovedByArticleIdAsync(long articleId)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        const string sql = """
            select id, article_id as ArticleId, author_name as AuthorName, text, created_date as CreatedDate, status
            from comments
            where article_id = @ArticleId and status = @Status
            order by created_date
            """;
        return await connection.QueryAsync<Comment>(sql, new { ArticleId = articleId, Status = (short)CommentStatus.Approved });
    }

    public async Task<Comment> CreateAsync(long articleId, PostCommentRequest request, CommentStatus status)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        const string sql = """
            insert into comments (article_id, author_name, text, status)
            values (@ArticleId, @AuthorName, @Text, @Status)
            returning id, article_id as ArticleId, author_name as AuthorName, text, created_date as CreatedDate, status
            """;

        return await connection.QuerySingleAsync<Comment>(sql, new
        {
            ArticleId = articleId,
            request.AuthorName,
            request.Text,
            Status = (short)status
        });
    }
}
