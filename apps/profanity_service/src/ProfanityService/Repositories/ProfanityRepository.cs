using Dapper;
using Npgsql;

namespace ProfanityService.Repositories;

public class ProfanityRepository : IProfanityRepository
{
    private readonly string _connectionString;
    private readonly ILogger<ProfanityRepository> _logger;

    public ProfanityRepository(
        IConfiguration configuration,
        ILogger<ProfanityRepository> logger)
    {
        _logger = logger;

        string connectionString = configuration.GetConnectionString("ProfanityDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:ProfanityDatabase is not configured.");

        string dbPassword = configuration["POSTGRES_DB_PWD"]
            ?? throw new InvalidOperationException("Missing profanity DB password.");

        NpgsqlConnectionStringBuilder connectionStringBuilder =
            new (connectionString)
            {
                Password = dbPassword
            };

        _connectionString = connectionStringBuilder.ConnectionString;
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
