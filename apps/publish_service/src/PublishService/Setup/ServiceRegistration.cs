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
        builder.Services.AddHttpClient<IHttpDraftClient, HttpDraftClient>(client =>
        {
            var baseUrl = builder.Configuration["DraftService:BaseUrl"]
                          ?? throw new InvalidOperationException(
                              "DraftService:BaseUrl is not configured.");

            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        builder.AddProfanityClient();
    }

    // Same retry + circuit breaker as DraftService and CommentService use for ProfanityService.
    private static void AddProfanityClient(this WebApplicationBuilder builder)
    {
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
                        // Exception type only - the message can contain URLs or payload details.
                        logger.LogWarning(
                            "Circuit {Pipeline} opened. Reason: {Reason}",
                            "ProfanityService", args.Outcome.Exception?.GetType().Name);

                        return ValueTask.CompletedTask;
                    },

                    OnClosed = _ =>
                    {
                        logger.LogInformation("Circuit {Pipeline} closed. Calls will resume.", "ProfanityService");

                        return ValueTask.CompletedTask;
                    },

                    OnHalfOpened = _ =>
                    {
                        logger.LogInformation("Circuit {Pipeline} half-open. Next request is a trial.", "ProfanityService");

                        return ValueTask.CompletedTask;
                    }
                });
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