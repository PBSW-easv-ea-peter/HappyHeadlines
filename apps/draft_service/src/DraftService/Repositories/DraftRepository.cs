using DraftService.Models;
using DraftService.Workflow;
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
        flagged_words as FlaggedWords, status, review_note as ReviewNote, byline as Byline
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

        var drafts = (await connection.QueryAsync<Draft>(sql, new { CreatedBy = createdBy })).ToList();
        await AttachCreditedJournalistsAsync(connection, drafts);
        return drafts;
    }

    public async Task<Draft?> GetByIdAsync(Guid id)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var sql = $"""
            select {SelectColumns}
            from drafts
            where id = @Id
            """;

        var draft = await connection.QueryFirstOrDefaultAsync<Draft>(sql, new { Id = id });
        if (draft is not null)
        {
            await AttachCreditedJournalistsAsync(connection, [draft]);
        }

        return draft;
    }

    public async Task<Draft> CreateAsync(CreateDraftRequest request)
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        var creditedJournalists = await GetJournalistsAsync(connection, request.CreditedJournalistIds);
        var byline = request.Byline ?? BylineGenerator.Generate(creditedJournalists);

        var sql = $"""
            insert into drafts
                (title, breadtext, location, section_id, created_by_journalist_id, last_edited_by_journalist_id, byline)
            values
                (@Title, @Breadtext, @Location, @SectionId, @JournalistId, @JournalistId, @Byline)
            returning {SelectColumns}
            """;

        var draft = await connection.QuerySingleAsync<Draft>(sql, new
        {
            request.Title,
            request.Breadtext,
            Location = request.Location.ToUpperInvariant(),
            request.SectionId,
            request.JournalistId,
            Byline = byline
        });

        await ReplaceCreditedJournalistsAsync(connection, draft.Id, request.CreditedJournalistIds);
        draft.CreditedJournalists = creditedJournalists;

        return draft;
    }

    public async Task<Draft?> UpdateContentAsync(Guid id, EditDraftRequest request)
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        var creditedJournalists = await GetJournalistsAsync(connection, request.CreditedJournalistIds);
        var byline = request.Byline ?? BylineGenerator.Generate(creditedJournalists);

        var sql = $"""
            update drafts
            set title = @Title,
                breadtext = @Breadtext,
                location = @Location,
                section_id = @SectionId,
                last_edited_by_journalist_id = @JournalistId,
                last_edited_date = current_timestamp,
                flagged_words = ARRAY[]::text[],
                byline = @Byline
            where id = @Id and status = @RequiredStatus
            returning {SelectColumns}
            """;

        var draft = await connection.QueryFirstOrDefaultAsync<Draft>(sql, new
        {
            Id = id,
            request.Title,
            request.Breadtext,
            Location = request.Location.ToUpperInvariant(),
            request.SectionId,
            request.JournalistId,
            Byline = byline,
            RequiredStatus = (short)DraftStatus.WorkInProgress
        });

        if (draft is null)
        {
            return null;
        }

        await ReplaceCreditedJournalistsAsync(connection, id, request.CreditedJournalistIds);
        draft.CreditedJournalists = creditedJournalists;

        return draft;
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

    private static async Task<List<Journalist>> GetJournalistsAsync(NpgsqlConnection connection, IReadOnlyList<long> journalistIds)
    {
        if (journalistIds.Count == 0)
        {
            return [];
        }

        const string sql = "select id, name from journalists where id = any(@Ids)";
        var journalists = await connection.QueryAsync<Journalist>(sql, new { Ids = journalistIds.ToArray() });
        return journalists.ToList();
    }

    private static async Task ReplaceCreditedJournalistsAsync(NpgsqlConnection connection, Guid draftId, IReadOnlyList<long> journalistIds)
    {
        await connection.ExecuteAsync("delete from draft_journalists where draft_id = @DraftId", new { DraftId = draftId });

        var distinctIds = journalistIds.Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return;
        }

        var rows = distinctIds.Select(journalistId => new { DraftId = draftId, JournalistId = journalistId });
        await connection.ExecuteAsync(
            "insert into draft_journalists (draft_id, journalist_id) values (@DraftId, @JournalistId)",
            rows);
    }

    private static async Task AttachCreditedJournalistsAsync(NpgsqlConnection connection, IReadOnlyList<Draft> drafts)
    {
        if (drafts.Count == 0)
        {
            return;
        }

        const string sql = """
            select dj.draft_id as DraftId, j.id as Id, j.name as Name
            from draft_journalists dj
            inner join journalists j on j.id = dj.journalist_id
            where dj.draft_id = any(@DraftIds)
            """;

        var rows = await connection.QueryAsync<CreditedJournalistRow>(sql, new { DraftIds = drafts.Select(d => d.Id).ToArray() });
        var byDraftId = rows
            .GroupBy(r => r.DraftId)
            .ToDictionary(g => g.Key, g => g.Select(r => new Journalist { Id = r.Id, Name = r.Name }).ToList());

        foreach (var draft in drafts)
        {
            draft.CreditedJournalists = byDraftId.TryGetValue(draft.Id, out var journalists) ? journalists : [];
        }
    }

    private sealed class CreditedJournalistRow
    {
        public Guid DraftId { get; set; }
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
