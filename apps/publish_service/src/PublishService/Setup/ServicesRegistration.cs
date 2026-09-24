using PublishService.Services.External;
using PublishService.Services.Handlers;

namespace PublishService.Setup;

public static class ServicesRegistration
{
    public static void AddLocalServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IDraftService, FakeDraftService>();
        builder.Services.AddScoped<IPublishHandler, PublishHandler>();
    }
}