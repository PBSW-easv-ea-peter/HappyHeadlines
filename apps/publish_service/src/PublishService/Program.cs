using HappyHeadlines.Observability;
using PublishService.Setup;
using PublishService.Shared.Options;

var builder = WebApplication.CreateBuilder(args);

// Load appsettings.{env}.jsonc. Sources added later win, so environment variables are added
// again afterwards - otherwise the file would override e.g. RabbitMQ__HostName from compose.
builder.Configuration
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.jsonc", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// OpenAPI
builder.ConfigureOpenApi();

// RabbitMQ
builder.ConfigureRabbitMq();

// Observability
builder.AddObservability();

// Cors
builder.ConfigureCors();

// Local Services
builder.AddLocalServices();
builder.Services.AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

await app.ConfigureExchangesAsync();
app.UseOpenApi();
app.UseCors(CorsPolicies.AllowWebApp);

app.MapEndpoints();

app.Run();
