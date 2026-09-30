group "Article" {
    articleService = container "ArticleService" "Provides published articles. (x-axis split: 3 load-balanced replicas via Docker Swarm, docker-compose.yaml)" "Service" "Implemented" {
        articlesController = component "ArticlesController" "Exposes REST CRUD endpoints for articles, scoped by location." "ASP.NET Core Controller"
        articleReadRepository = component "ArticleReadRepository" "Reads articles from the resolved shard." "Repository"
        articleWriteRepository = component "ArticleWriteRepository" "Creates, updates and deletes articles in the resolved shard (REST stand-ins for Create/Update)." "Repository"
        articleShardResolver = component "ArticleShardResolver" "Resolves a location code to the correct shard connection string." "Component"
        articleQueueConsumer = component "ArticleQueueConsumer" "Will consume ArticleQueue for Create/Update once wired up. Currently idle." "Background Service"
        articleObservability = component "Observability" "Shared library HappyHeadlines.Observability: AddObservability() configures OpenTelemetry logging and tracing (ASP.NET Core, HttpClient, Npgsql)." "Shared library"

        articlesController -> articleReadRepository "Delegates GET requests to"
        articlesController -> articleWriteRepository "Delegates Create/Update/Delete REST stand-ins to"
        articleReadRepository -> articleShardResolver "Resolves shard via"
        articleWriteRepository -> articleShardResolver "Resolves shard via"
        articleQueueConsumer -> articleWriteRepository "Will persist consumed messages via (not yet wired)"
    }
    articleDb = container "ArticleDatabase" "Stores published articles. (z-axis split: sharded per continent, 8 instances)" "PostgreSQL" "Database,Implemented"
}
