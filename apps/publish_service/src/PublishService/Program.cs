using PublishService.Setup;
using PublishService.Endpoints;
using PublishService.Models.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// OpenAPI
builder.ConfigureOpenApi();

// RabbitMQ
builder.ConfigureRabbitMq();

var app = builder.Build();

await app.ConfigureExchangesAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsPolicies.AllowWebApp);

app.MapPublishActionsEndpoints();

app.Run();
