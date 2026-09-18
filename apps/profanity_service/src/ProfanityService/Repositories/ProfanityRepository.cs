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

    public async Task<IReadOnlyList<string>> FindBannedWordsAsync(IEnumerable<string> words)
    {
        var lowercasedWords = words.Select(w => w.ToLowerInvariant()).ToArray();

        await using var connection = new NpgsqlConnection(_connectionString);
        const string sql = "select word from banned_words where word = ANY(@Words)";
        var matches = await connection.QueryAsync<string>(sql, new { Words = lowercasedWords });
        return matches.ToList();
    }
}
