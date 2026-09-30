group "Comment" {
    commentService = container "CommentService" "Manages comments." "Service" "Implemented" {
        commentsController = component "CommentsController" "Exposes REST endpoints for posting and retrieving comments, scoped by article location." "ASP.NET Core Controller"
        commentHandler = component "CommentHandler" "Classifies comment text via ProfanityService (Approved / Rejected / PendingProfanityCheck) and orchestrates persistence." "Component"
        commentRepository = component "CommentRepository" "Reads and writes comments (incl. article_id/article_location) via Dapper." "Repository"
        profanityClient = component "ProfanityClient" "Calls ProfanityService directly over HTTP, wrapped in a Polly retry + circuit breaker. Fallback: comment is stored as PendingProfanityCheck." "Component"
        commentObservability = component "Observability" "Shared library HappyHeadlines.Observability: AddObservability() configures OpenTelemetry logging and tracing (ASP.NET Core, HttpClient, Npgsql)." "Shared library"

        commentsController -> commentHandler "Delegates classification and persistence to"
        commentHandler -> profanityClient "Checks comment text via"
        commentHandler -> commentRepository "Persists and reads comments via"
    }
    commentDb = container "CommentDatabase" "Stores comments." "PostgreSQL" "Database,Implemented"
}
