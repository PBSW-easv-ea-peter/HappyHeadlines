using DraftService.Models;
using DraftService.Workflow;
using Dapper;
using Npgsql;

namespace DraftService.Repositories;

public class JournalistRepository : IJournalistRepository
{
    private readonly string _connectionString;
    private readonly ILogger<JournalistRepository> _logger;

    public JournalistRepository(
        IConfiguration configuration,
        ILogger<JournalistRepository> logger)
    {
        _logger = logger;
        
        string connectionString = configuration.GetConnectionString("DraftDatabase") 
                                  ?? throw new InvalidOperationException("ConnectionStrings:DraftDatabase is not configured.");

        string dbPassword = configuration["POSTGRES_DB_PWD"]
                            ?? throw new InvalidOperationException("Missing Draft DB password.");

        NpgsqlConnectionStringBuilder connectionStringBuilder =
            new (connectionString)
            {
                Password = dbPassword
            };

        _connectionString = connectionStringBuilder.ConnectionString;
        _logger.LogWarning("Draft CS: {cs}", _connectionString);
    }

    public async Task<IEnumerable<Journalist>> GetAllAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        const string sql = "select id, name, email, phone from journalists";

        var journalists = await connection.QueryAsync<Journalist>(sql);
        return journalists.OrderBy(BylineGenerator.LastName, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyCollection<long>> FindExistingIdsAsync(IReadOnlyCollection<long> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        const string sql = "select id from journalists where id = any(@Ids)";

        var existingIds = await connection.QueryAsync<long>(sql, new { Ids = ids.ToArray() });
        return existingIds.ToList();
    }
}
