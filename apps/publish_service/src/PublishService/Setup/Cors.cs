using PublishService.Models.Options;

namespace PublishService.Setup;

public static class Cors
{
    public static WebApplicationBuilder ConfigureCors(
        this WebApplicationBuilder builder)
    {
        builder.AddClientPolicy();

        return builder;
    }

    private static WebApplicationBuilder AddClientPolicy(
        this WebApplicationBuilder builder)
    {
        var webAppBaseUrl = builder.Configuration["WebApp:BaseUrl"] 
                            ?? throw new InvalidOperationException("WebApp:BaseUrl is not configured.");

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicies.AllowWebApp, policy =>
            {
                policy
                    .WithOrigins(webAppBaseUrl)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
        
        return builder;
    }
}