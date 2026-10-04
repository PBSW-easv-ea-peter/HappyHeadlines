using ArticleService.Cache;
using StackExchange.Redis;

namespace ArticleService.Setup;

public static class Cache
{
    public static void ConfigureCache(this WebApplicationBuilder builder)
    {
        string redisConnectionString = builder.Configuration["Cache:RedisConnection"]
            ?? "article-cache:6379";

        // ArticleService must start and serve articles from the database without Redis, so
        // startup doesn't wait for it (it keeps reconnecting in the background) and commands
        // fail fast instead of queueing while it's down - ArticleCache treats that as a miss.
        ConfigurationOptions redisOptions = ConfigurationOptions.Parse(redisConnectionString);
        redisOptions.AbortOnConnectFail = false;
        redisOptions.BacklogPolicy = BacklogPolicy.FailFast;
        redisOptions.ConnectTimeout = 2000;
        redisOptions.SyncTimeout = 1000;
        redisOptions.AsyncTimeout = 1000;

        builder.Services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect(redisOptions));

        builder.Services.AddScoped<IArticleCache, ArticleCache>();
    }
}
