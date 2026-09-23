namespace PublishService.Setup;

public static class OpenApi
{
    public static WebApplicationBuilder ConfigureOpenApi(
        this WebApplicationBuilder builder)
    {
        builder
            .AddSwagger()
            .AddOpenApi();
        
        return builder;
    }

    private static WebApplicationBuilder AddSwagger(
        this WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerGen();
        return builder;
    }

    private static WebApplicationBuilder AddOpenApi(
        this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi();
        return builder;
    }
}