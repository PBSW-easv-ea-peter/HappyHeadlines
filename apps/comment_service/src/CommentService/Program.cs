using CommentService.Cache;
using CommentService.Handlers;
using CommentService.Profanity;
using CommentService.Repositories;
using HappyHeadlines.Observability;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI
builder.Services.AddOpenApi();

// Swagger
builder.Services.AddSwaggerGen();

builder.AddObservability();

builder.Services.AddControllers();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ICommentHandler, CommentHandler>();

// CommentCache (docs/Caching.md). The service must keep working without it, so startup
// doesn't wait for Redis and commands fail fast instead of queueing while it's down -
// RedisCommentCache then falls back to the database.
var redisOptions = ConfigurationOptions.Parse(
    builder.Configuration["Cache:RedisConnection"]
        ?? throw new InvalidOperationException("Cache:RedisConnection is not configured."));
redisOptions.AbortOnConnectFail = false;
redisOptions.BacklogPolicy = BacklogPolicy.FailFast;
redisOptions.ConnectTimeout = 2000;
redisOptions.SyncTimeout = 1000;
redisOptions.AsyncTimeout = 1000;

builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisOptions));
builder.Services.AddSingleton<ICommentCache, RedisCommentCache>();

var webAppBaseUrl = builder.Configuration["WebApp:BaseUrl"]
    ?? throw new InvalidOperationException("WebApp:BaseUrl is not configured.");
var readerWebBaseUrl = builder.Configuration["ReaderWeb:BaseUrl"]
    ?? throw new InvalidOperationException("ReaderWeb:BaseUrl is not configured.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        policy
            .WithOrigins(webAppBaseUrl, readerWebBaseUrl)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// // Create a resilience pipeline with a retry and circuit breaker
// builder.Services.AddResiliencePipeline(
//     "ProfanityService",
//     pipeline =>
//     {
//         pipeline
//             .AddRetry(new RetryStrategyOptions
//             {
//                 ShouldHandle = new PredicateBuilder()
//                     .Handle<HttpRequestException>()
//                     .Handle<TaskCanceledException>(),
//                 MaxRetryAttempts = 2
//             })
//             .AddCircuitBreaker(new CircuitBreakerStrategyOptions
//             {
//                 // Break after 2 consecutive failures
//                 FailureRatio = 1.0, // 100% of the last N calls must fail to break
//                 MinimumThroughput = 2, // Minimum number of calls to evaluate
//                 SamplingDuration = TimeSpan.FromSeconds(30), // Time window to evaluate failures
//                 BreakDuration = TimeSpan.FromSeconds(10), // How long the circuit stays open
//                 ShouldHandle = new PredicateBuilder()
//                     .Handle<HttpRequestException>() // Handle HTTP failures
//                     .Handle<TaskCanceledException>(), // Handle timeouts
//                 OnOpened = args =>
//                 {
//                     Console.WriteLine($"Circuit broken! Reason: {args.Outcome.Exception?.Message}. Will not call profanityservice for 30 seconds.");
//                     return ValueTask.CompletedTask;
//                 },
//                 OnClosed = args =>
//                 {
//                     Console.WriteLine("Circuit reset! Will retry calls to profanityservice.");
//                     return ValueTask.CompletedTask;
//                 },
//                 OnHalfOpened = (context) =>
//                 {
//                     Console.WriteLine("Circuit half-open; next call to profanityservice is a trial.");
//                     return ValueTask.CompletedTask;
//                 }
//             });
//     });

builder.Services
    .AddHttpClient<IProfanityClient, ProfanityClient>(client =>
    {
        var baseUrl = builder.Configuration["ProfanityService:BaseUrl"]
            ?? throw new InvalidOperationException(
                "ProfanityService:BaseUrl is not configured.");

        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(2);
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .AddResilienceHandler("ProfanityService", (pipeline, context) =>
    {
        // Circuit state changes go through ILogger so they reach Loki (docs/logging.md).
        var logger = context.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("ProfanityService.CircuitBreaker");

        pipeline.AddRetry(new HttpRetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .Handle<TaskCanceledException>(),

            // Initial request + 2 retries = 3 total attempts.
            MaxRetryAttempts = 2
        });

        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            // Two failed operations out of two → open circuit.
            FailureRatio = 1.0,
            MinimumThroughput = 2,

            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(10),

            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .Handle<TaskCanceledException>(),

            OnOpened = args =>
            {
                // Exception type only - the message can contain URLs or payload details.
                logger.LogWarning(
                    "Circuit {Pipeline} opened. Reason: {Reason}",
                    "ProfanityService", args.Outcome.Exception?.GetType().Name);

                return ValueTask.CompletedTask;
            },

            OnClosed = args =>
            {
                logger.LogInformation("Circuit {Pipeline} closed. Calls will resume.", "ProfanityService");

                return ValueTask.CompletedTask;
            },

            OnHalfOpened = args =>
            {
                logger.LogInformation("Circuit {Pipeline} half-open. Next request is a trial.", "ProfanityService");

                return ValueTask.CompletedTask;
            }
        });
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowWebApp");

app.MapControllers();

app.Run();
