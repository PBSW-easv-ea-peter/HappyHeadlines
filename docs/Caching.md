Caches: Redis
ArticleCache:
- Refresh at midnight
- Batch-refresh
- Articles cached: Last 14 days

CommentCache:
- Cached comments: ALL / 30 recently accessed articles
- Write-through caching
- Refresh at miss
- LRU eviction

Caching dashboard:
- historisk søjlediagram for hit/miss ratio pr. cache
- Pie-diagram for cache-status