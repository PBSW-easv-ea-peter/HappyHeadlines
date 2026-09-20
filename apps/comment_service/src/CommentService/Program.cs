using CommentService.Handlers;
using CommentService.Profanity;
using CommentService.Repositories;
using Microsoft.Extensions.Http.Resilience;
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

var webAppBaseUrl = builder.Configuration["WebApp:BaseUrl"]
    ?? throw new InvalidOperationException("WebApp:BaseUrl is not configured.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        policy
            .WithOrigins(webAppBaseUrl)
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
    .AddResilienceHandler("ProfanityService", pipeline =>
    {
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
                Console.WriteLine(
                    $"Circuit opened! Reason: " +
                    $"{args.Outcome.Exception?.Message}");

                return ValueTask.CompletedTask;
            },

            OnClosed = args =>
            {
                Console.WriteLine(
                    "Circuit closed. Calls to ProfanityService will resume.");

                return ValueTask.CompletedTask;
            },

            OnHalfOpened = args =>
            {
                Console.WriteLine(
                    "Circuit half-open. Next request is a trial.");

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
