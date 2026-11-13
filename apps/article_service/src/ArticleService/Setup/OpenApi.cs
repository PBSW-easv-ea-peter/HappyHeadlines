namespace ArticleService.Setup;

public static class OpenApi
{
    public static void ConfigureOpenApi(this WebApplicationBuilder builder)
    {
        builder
            .AddSwagger()
            .AddOpenApi();
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

    public static WebApplication UseOpenApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();

            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }

}
