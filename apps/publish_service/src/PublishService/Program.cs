using PublishService.Setup;
using PublishService.Endpoints;
using PublishService.Models.Options;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI
builder.ConfigureOpenApi();

// RabbitMQ
builder.ConfigureRabbitMq();

// Telemetry
builder.ConfigureOpenTelemetry();

// Cors
builder.ConfigureCors();

// Local Services
builder.AddLocalServices();

var app = builder.Build();

await app.ConfigureExchangesAsync();
app.UseOpenApi();
app.UseCors(CorsPolicies.AllowWebApp);
app.MapPublishActionsEndpoints();

app.Run();
