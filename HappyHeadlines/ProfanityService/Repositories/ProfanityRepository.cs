using Dapper;
using Npgsql;

namespace ProfanityService.Repositories;

public class ProfanityRepository : IProfanityRepository
{
    private readonly string _connectionString;

    public ProfanityRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ProfanityDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:ProfanityDatabase is not configured.");
    }

    public async Task<bool> IsProfaneAsync(string word)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        const string sql = "select exists(select 1 from banned_words where word = lower(@Word))";
        return await connection.ExecuteScalarAsync<bool>(sql, new { Word = word });
    }
}
