group "Profanity" {
    profanityService = container "ProfanityService" "Filters inappropriate language." "Service" "Implemented" {
        profanityController = component "ProfanityController" "Exposes REST endpoint POST /api/profanity/check that takes a text and returns the banned words found in it." "ASP.NET Core Controller"
        profanityChecker = component "ProfanityChecker" "Splits the text into distinct words (case-insensitive) and delegates the lookup." "Component"
        profanityRepository = component "ProfanityRepository" "Looks up all words in banned_words in one query via Dapper." "Repository"
        profanityObservability = component "Observability" "Shared library HappyHeadlines.Observability: AddObservability() configures OpenTelemetry logging and tracing (ASP.NET Core, HttpClient, Npgsql)." "Shared library"

        profanityController -> profanityChecker "Delegates the check to"
        profanityChecker -> profanityRepository "Looks up words via"
    }
    profanityDb = container "ProfanityDatabase" "Stores prohibited words." "PostgreSQL" "Database,Implemented"
}
