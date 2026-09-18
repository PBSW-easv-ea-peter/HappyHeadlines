using CommentService.Handlers;
using CommentService.Profanity;
using CommentService.Repositories;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI
builder.Services.AddOpenApi();

// Swagger
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ICommentHandler, CommentHandler>();

// Create a resilience pipeline with a retry and circuit breaker
builder.Services.AddResiliencePipeline(
    "ProfanityService",
    pipeline =>
    {
        pipeline
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>(),
                MaxRetryAttempts = 2
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                // Break after 2 consecutive failures
                FailureRatio = 1.0, // 100% of the last N calls must fail to break
                MinimumThroughput = 2, // Minimum number of calls to evaluate
                SamplingDuration = TimeSpan.FromSeconds(30), // Time window to evaluate failures
                BreakDuration = TimeSpan.FromSeconds(10), // How long the circuit stays open
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>() // Handle HTTP failures
                    .Handle<TaskCanceledException>(), // Handle timeouts
                OnOpened = args =>
                {
                    Console.WriteLine($"Circuit broken! Reason: {args.Outcome.Exception?.Message}. Will not call profanityservice for 30 seconds.");
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    Console.WriteLine("Circuit reset! Will retry calls to profanityservice.");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = (context) =>
                {
                    Console.WriteLine("Circuit half-open; next call to profanityservice is a trial.");
                    return ValueTask.CompletedTask;
                }
            });
    });

// Direct HTTP call to ProfanityService - no gateway or UI in between, per this week's
// requirement. The timeout keeps a hanging ProfanityService from blocking CommentService's
// own swim lane; the retry/circuit-breaker policies live in ProfanityClient itself.
builder.Services.AddHttpClient<IProfanityClient, ProfanityClient>(client =>
{
    var baseUrl = builder.Configuration["ProfanityService:BaseUrl"]
        ?? throw new InvalidOperationException("ProfanityService:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(2);
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
