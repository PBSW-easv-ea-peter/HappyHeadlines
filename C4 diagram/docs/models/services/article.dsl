group "Article" {

    articleService = container "ArticleService" "Provides published articles. (x-axis split: 3 load-balanced replicas via Docker Swarm, docker-compose.yaml)" "Service" "Implemented" {
        articleCache = component "ArticleCache" "Implements IArticleCache: reads and writes cached articles (keys per location and per article) in Redis, with a 14-day expiry." "Component"
        cacheRefreshService = component "CacheRefreshService" "Refills the cache every hour. Currently only location GO (global)." "Background Service"
        articlesController = component "ArticlesController" "Exposes REST CRUD endpoints for articles, scoped by location." "ASP.NET Core Controller"
        articleReadRepository = component "ArticleReadRepository" "Reads articles from the resolved shard." "Repository"
        articleWriteRepository = component "ArticleWriteRepository" "Creates, updates and deletes articles in the resolved shard (REST stand-ins for Create/Update)." "Repository"
        articleShardResolver = component "ArticleShardResolver" "Resolves a location code to the correct shard connection string." "Component"
        articleQueueConsumer = component "ArticleQueueConsumer" "Will consume RabbitMQ for Create/Update once wired up. Currently idle." "Background Service"
        articleObservability = component "Observability" "Shared library HappyHeadlines.Observability: AddObservability() configures OpenTelemetry logging and tracing (ASP.NET Core, HttpClient, Npgsql)." "Shared library"

        articlesController -> articleCache "Reads articles from cache first, invalidates on update/delete via"
        articlesController -> articleReadRepository "Delegates GET requests to"
        articlesController -> articleWriteRepository "Delegates Create/Update/Delete REST stand-ins to"
        articleReadRepository -> articleShardResolver "Resolves shard via"
        articleWriteRepository -> articleShardResolver "Resolves shard via"
        articleQueueConsumer -> articleWriteRepository "Will persist consumed messages via (not yet wired)"
        cacheRefreshService -> articleCache "Refreshes every hour via"
        articleCache -> articleReadRepository "Loads articles for refresh via"

    }
    articleDb = container "ArticleDatabase" "Stores published articles. (z-axis split: sharded per continent, 8 instances)" "PostgreSQL" "Database,Implemented"
    articleCacheDb = container "ArticleCache" "Offline cache of articles from the last 14 days, refilled every hour. A miss reads the database but does not fill the cache. Shared by all ArticleService replicas." "Redis" "Database,Implemented"

}
