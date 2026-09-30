using HappyHeadlines.Observability;
using PublishService.Setup;
using PublishService.Shared.Options;

var builder = WebApplication.CreateBuilder(args);

// Load appsettings.jsonc (base configuration)
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    // .AddJsonFile("appsettings.jsonc", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.jsonc", optional: true, reloadOnChange: true)
    .Build();

// Assign the configuration to the builder
builder.Configuration.AddConfiguration(config);

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
