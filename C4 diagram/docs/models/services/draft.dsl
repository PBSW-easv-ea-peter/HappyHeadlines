// Swimlanes (fault isolation): each implemented service is grouped with its own
// database, so a failure in one lane cannot take down another lane's data.
group "Draft" {
    draftService = container "DraftService" "Manages article drafts and their editorial workflow." "Service" "Implemented" {
        draftsController = component "DraftsController" "Exposes REST endpoints for drafts and workflow actions (submit, approve, reject, publish, archive, reactivate)." "ASP.NET Core Controller"
        draftHandler = component "DraftHandler" "Orchestrates draft business rules, profanity pre-check on submit, and persistence. Logs every status transition (docs/logging.md)." "Component"
        draftStatusTransitions = component "DraftStatusTransitions" "Single source of truth for legal draft status transitions." "Component"
        draftRepository = component "DraftRepository" "Reads and writes drafts via Dapper." "Repository"
        draftProfanityClient = component "ProfanityClient" "Calls ProfanityService directly over HTTP, wrapped in a Polly retry + circuit breaker + per-attempt timeout. Fallback: submission succeeds and the draft is marked profanityCheckSkipped, so the editor is warned instead of told 'no issues found'." "Component"
        draftObservability = component "Observability" "Shared library HappyHeadlines.Observability: AddObservability() configures OpenTelemetry logging and tracing (ASP.NET Core, HttpClient, Npgsql)." "Shared library"

        draftsController -> draftHandler "Delegates requests to"
        draftHandler -> draftStatusTransitions "Validates status transitions via"
        draftHandler -> draftProfanityClient "Pre-flags words on submit via"
        draftHandler -> draftRepository "Persists and reads drafts via"
    }
    draftDb = container "DraftDatabase" "Stores article drafts." "PostgreSQL" "Database,Implemented"
}
