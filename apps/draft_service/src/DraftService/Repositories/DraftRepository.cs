using DraftService.Models;
using Dapper;
using Npgsql;

namespace DraftService.Repositories;

public class DraftRepository : IDraftRepository
{
    private readonly string _connectionString;

    public DraftRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DraftDatabase")
            ?? throw new InvalidOperationException("ConnectionStrings:DraftDatabase is not configured.");
    }

    private const string SelectColumns = """
        id, title, breadtext, location, section_id as SectionId,
        created_by_journalist_id as CreatedByJournalistId, created_date as CreatedDate,
        last_edited_by_journalist_id as LastEditedByJournalistId, last_edited_date as LastEditedDate,
        approved_by_journalist_id as ApprovedByJournalistId, approved_date as ApprovedDate,
        flagged_words as FlaggedWords, status, review_note as ReviewNote
        """;

    public async Task<IEnumerable<Draft>> GetAllAsync(long? createdBy)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var sql = $"""
            select {SelectColumns}
            from drafts
            where @CreatedBy is null or created_by_journalist_id = @CreatedBy
            order by created_date desc
            """;

        return await connection.QueryAsync<Draft>(sql, new { CreatedBy = createdBy });
    }

    public async Task<Draft?> GetByIdAsync(Guid id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var sql = $"""
            select {SelectColumns}
            from drafts
            where id = @Id
            """;

        return await connection.QueryFirstOrDefaultAsync<Draft>(sql, new { Id = id });
    }

    public async Task<Draft> CreateAsync(CreateDraftRequest request)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var sql = $"""
            insert into drafts
                (title, breadtext, location, section_id, created_by_journalist_id, last_edited_by_journalist_id)
            values
                (@Title, @Breadtext, @Location, @SectionId, @JournalistId, @JournalistId)
            returning {SelectColumns}
            """;

        return await connection.QuerySingleAsync<Draft>(sql, new
        {
            request.Title,
            request.Breadtext,
            Location = request.Location.ToUpperInvariant(),
            request.SectionId,
            request.JournalistId
        });
    }

    public async Task<Draft?> UpdateContentAsync(Guid id, EditDraftRequest request)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var sql = $"""
            update drafts
            set title = @Title,
                breadtext = @Breadtext,
                location = @Location,
                section_id = @SectionId,
                last_edited_by_journalist_id = @JournalistId,
                last_edited_date = current_timestamp,
                flagged_words = ARRAY[]::text[]
            where id = @Id and status = @RequiredStatus
            returning {SelectColumns}
            """;

        return await connection.QueryFirstOrDefaultAsync<Draft>(sql, new
        {
            Id = id,
            request.Title,
            request.Breadtext,
            Location = request.Location.ToUpperInvariant(),
            request.SectionId,
            request.JournalistId,
            RequiredStatus = (short)DraftStatus.WorkInProgress
        });
    }

    public async Task<Draft?> SubmitForApprovalAsync(Guid id, DraftStatus expectedCurrentStatus, IReadOnlyList<string> flaggedWords)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var sql = $"""
            update drafts
            set status = @NewStatus,
                flagged_words = @FlaggedWords,
                review_note = null
            where id = @Id and status = @ExpectedCurrentStatus
            returning {SelectColumns}
            """;

        return await connection.QueryFirstOrDefaultAsync<Draft>(sql, new
        {
            Id = id,
            NewStatus = (short)DraftStatus.PendingApproval,
            FlaggedWords = flaggedWords.ToArray(),
            ExpectedCurrentStatus = (short)expectedCurrentStatus
        });
    }

    public async Task<Draft?> UpdateStatusAsync(Guid id, DraftStatus expectedCurrentStatus, DraftStatus newStatus)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var sql = $"""
            update drafts
            set status = @NewStatus
            where id = @Id and status = @ExpectedCurrentStatus
            returning {SelectColumns}
            """;

        return await connection.QueryFirstOrDefaultAsync<Draft>(sql, new
        {
            Id = id,
            NewStatus = (short)newStatus,
            ExpectedCurrentStatus = (short)expectedCurrentStatus
        });
    }

    public async Task<Draft?> ApproveAsync(Guid id, DraftStatus expectedCurrentStatus, long approvedByJournalistId, string? note)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var sql = $"""
            update drafts
            set status = @NewStatus,
                approved_by_journalist_id = @ApprovedByJournalistId,
                approved_date = current_timestamp,
                review_note = @Note
            where id = @Id and status = @ExpectedCurrentStatus
            returning {SelectColumns}
            """;

        return await connection.QueryFirstOrDefaultAsync<Draft>(sql, new
        {
            Id = id,
            NewStatus = (short)DraftStatus.Approved,
            ApprovedByJournalistId = approvedByJournalistId,
            Note = note,
            ExpectedCurrentStatus = (short)expectedCurrentStatus
        });
    }

    public async Task<Draft?> RejectAsync(Guid id, DraftStatus expectedCurrentStatus, string? note)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var sql = $"""
            update drafts
            set status = @NewStatus,
                review_note = @Note
            where id = @Id and status = @ExpectedCurrentStatus
            returning {SelectColumns}
            """;

        return await connection.QueryFirstOrDefaultAsync<Draft>(sql, new
        {
            Id = id,
            NewStatus = (short)DraftStatus.WorkInProgress,
            Note = note,
            ExpectedCurrentStatus = (short)expectedCurrentStatus
        });
    }
}
