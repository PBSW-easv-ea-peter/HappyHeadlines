using DraftService.Handlers;
using DraftService.Profanity;
using DraftService.Repositories;
using HappyHeadlines.Observability;
using Microsoft.Extensions.Http.Resilience;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI
builder.Services.AddOpenApi();

// Swagger
builder.Services.AddSwaggerGen();

builder.AddObservability();

builder.Services.AddControllers();
builder.Services.AddScoped<IDraftRepository, DraftRepository>();
builder.Services.AddScoped<IJournalistRepository, JournalistRepository>();
builder.Services.AddScoped<IDraftHandler, DraftHandler>();

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

builder.Services
    .AddHttpClient<IProfanityClient, ProfanityClient>(client =>
    {
        var baseUrl = builder.Configuration["ProfanityService:BaseUrl"]
            ?? throw new InvalidOperationException("ProfanityService:BaseUrl is not configured.");

        client.BaseAddress = new Uri(baseUrl);
        // Covers all attempts together, so it only backstops the pipeline - the per-attempt
        // timeout at the end of the pipeline is what cuts a slow call short.
        client.Timeout = TimeSpan.FromSeconds(10);
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
            // Default ShouldHandle: exceptions, 5xx, 408, 429 and attempt timeouts - the circuit
            // breaker uses the same, so an erroring ProfanityService also opens the circuit.
            // Initial request + 2 retries = 3 total attempts.
            MaxRetryAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential
        });

        pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            // Two failed operations out of two -> open circuit.
            FailureRatio = 1.0,
            MinimumThroughput = 2,

            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(10),

            OnOpened = args =>
            {
                // Exception type only - the message can contain URLs or payload details.
                logger.LogWarning(
                    "Circuit {Pipeline} opened. Reason: {Reason}",
                    "ProfanityService",
                    args.Outcome.Exception?.GetType().Name ?? $"HTTP {(int?)args.Outcome.Result?.StatusCode}");

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

        pipeline.AddTimeout(TimeSpan.FromSeconds(2));
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
