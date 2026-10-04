using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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