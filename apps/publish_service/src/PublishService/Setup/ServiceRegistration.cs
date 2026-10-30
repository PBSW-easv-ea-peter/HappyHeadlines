using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using PublishService.Features.Publish;
using PublishService.Shared;
using PublishService.Shared.External;
using PublishService.Shared.HttpClients;

namespace PublishService.Setup;

public static class ServiceRegistration
{
    public static void AddLocalServices(this WebApplicationBuilder builder)
    {
        // Handlers
        builder.Services.AddScoped<IPublishDraftHandler, PublishDraftHandler>();

        // HttpClients
        builder.Services
            .AddHttpClient<IHttpDraftClient, HttpDraftClient>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["DraftService:BaseUrl"]
                    ?? throw new InvalidOperationException("DraftService:BaseUrl is not configured."));
                client.Timeout = OverallTimeout;
            })
            .AddDependencyResilience("DraftService");

        builder.Services
            .AddHttpClient<IProfanityClient, ProfanityClient>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["ProfanityService:BaseUrl"]
                    ?? throw new InvalidOperationException("ProfanityService:BaseUrl is not configured."));
                client.Timeout = OverallTimeout;
            })
            .AddDependencyResilience("ProfanityService");
    }

    // HttpClient.Timeout covers all attempts together, so it only backstops the pipeline.
    // The per-attempt timeout is what actually cuts a slow call short.
    private static readonly TimeSpan OverallTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(2);

    // Retry + circuit breaker + per-attempt timeout, like DraftService and CommentService use for
    // ProfanityService. Unlike theirs, the default ShouldHandle is kept, so 5xx responses and
    // attempt timeouts count as failures too - not only exceptions.
    private static void AddDependencyResilience(this IHttpClientBuilder client, string pipelineName)
    {
        client
            .SetHandlerLifetime(TimeSpan.FromMinutes(5))
            .AddResilienceHandler(pipelineName, (pipeline, context) =>
            {
                // Circuit state changes go through ILogger so they reach Loki (docs/logging.md).
                var logger = context.ServiceProvider
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger($"{pipelineName}.CircuitBreaker");

                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    // Initial request + 2 retries = 3 total attempts, close together so the
                    // caller isn't kept waiting.
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
                        // Exception type or status code only - an exception message can contain
                        // URLs or payload details.
                        logger.LogWarning(
                            "Circuit {Pipeline} opened. Reason: {Reason}",
                            pipelineName,
                            args.Outcome.Exception?.GetType().Name ?? $"HTTP {(int?)args.Outcome.Result?.StatusCode}");

                        return ValueTask.CompletedTask;
                    },

                    OnClosed = _ =>
                    {
                        logger.LogInformation("Circuit {Pipeline} closed. Calls will resume.", pipelineName);

                        return ValueTask.CompletedTask;
                    },

                    OnHalfOpened = _ =>
                    {
                        logger.LogInformation("Circuit {Pipeline} half-open. Next request is a trial.", pipelineName);

                        return ValueTask.CompletedTask;
                    }
                });

                pipeline.AddTimeout(AttemptTimeout);
            });
    }
    
    // Add endpoints
    public static IServiceCollection AddEndpoints(
        this IServiceCollection services,
        Assembly assembly)
    {
        ServiceDescriptor[] serviceDescriptors =
        [
            .. assembly
                .DefinedTypes
                .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                               type.IsAssignableTo(typeof(IEndpoint)))
                .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))
        ];

        services.TryAddEnumerable(serviceDescriptors);

        return services;
    }
    
    // Map endpoints
    public static IApplicationBuilder MapEndpoints(
        this WebApplication app,
        RouteGroupBuilder? routeGroupBuilder = null)
    {
        IEnumerable<IEndpoint> endpoints = app.Services
            .GetRequiredService<IEnumerable<IEndpoint>>();

        IEndpointRouteBuilder builder =
            routeGroupBuilder is null ? app : routeGroupBuilder;

        foreach (IEndpoint endpoint in endpoints)
        {
            endpoint.MapEndpoint(builder);
        }

        return app;
    }
}