
// ArticleService component-level relations (imply the ArticleService container-level
// relations to RabbitMQ/ArticleDatabase, so no separate container-level ones here)
articleReadRepository -> articleDb "Reads articles from"
articleWriteRepository -> articleDb "Writes articles to"
articleQueueConsumer -> publishedArticlesExchange "Subscribes to (idle - not wired up yet)"
articleObservability -> otelForwarder "Exports logs and traces via" "OTLP/HTTP"
articleCache -> articleCacheDb "Reads and writes cached articles in" "Redis"
