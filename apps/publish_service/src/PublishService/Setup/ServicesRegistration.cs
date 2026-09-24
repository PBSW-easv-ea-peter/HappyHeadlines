using PublishService.Services.External;
using PublishService.Services.Handlers;

namespace PublishService.Setup;

public static class ServicesRegistration
{
    public static void AddLocalServices(this WebApplicationBuilder builder)
    {
        // builder.Services.AddScoped<IDraftService, FakeDraftService>();
        builder.Services.AddScoped<IPublishHandler, PublishHandler>();
        
        // HttpClients
        builder.Services.AddHttpClient<IDraftService, DraftClient>(client =>
        {
            var baseUrl = builder.Configuration["DraftService:BaseUrl"]
                          ?? throw new InvalidOperationException(
                              "DraftService:BaseUrl is not configured.");

            client.BaseAddress = new Uri(baseUrl);
        });
    }
}