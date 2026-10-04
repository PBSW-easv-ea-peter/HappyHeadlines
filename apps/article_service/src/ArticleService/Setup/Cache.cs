using ArticleService.Cache;
using StackExchange.Redis;

namespace ArticleService.Setup;

public static class Cache
{
    public static void ConfigureCache(this WebApplicationBuilder builder)
    {
        string redisConnectionString = builder.Configuration["Cache:RedisConnection"]
            ?? "article-cache:6379";

        builder.Services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisConnectionString));

        builder.Services.AddScoped<IArticleCache, ArticleCache>();
    }
}