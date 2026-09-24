using DraftService.Models;
using DraftService.Workflow;
using Dapper;
using Npgsql;

namespace DraftService.Repositories;

public class JournalistRepository : IJournalistRepository
{
    private readonly string _connectionString;

    public JournalistRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DraftDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:DraftDatabase is not configured.");
    }

    public async Task<IEnumerable<Journalist>> GetAllAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        const string sql = "select id, name, email, phone from journalists";

        var journalists = await connection.QueryAsync<Journalist>(sql);
        return journalists.OrderBy(BylineGenerator.LastName, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> ExistsAsync(long id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        const string sql = "select exists(select 1 from journalists where id = @Id)";

        return await connection.ExecuteScalarAsync<bool>(sql, new { Id = id });
    }
}
