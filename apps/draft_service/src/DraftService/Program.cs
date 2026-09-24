using DraftService.Handlers;
using DraftService.Profanity;
using DraftService.Repositories;
using DraftService.Setup;
using Microsoft.Extensions.Http.Resilience;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI
builder.Services.AddOpenApi();

// Swagger
builder.Services.AddSwaggerGen();

builder.ConfigureOpenTelemetry();

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
            // Two failed operations out of two -> open circuit.
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
