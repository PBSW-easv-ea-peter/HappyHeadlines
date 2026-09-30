using PublishService.Setup;
<<<<<<< HEAD
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

=======
using PublishService.Endpoints;
using PublishService.Models.Options;
using HappyHeadlines.Observability;

var builder = WebApplication.CreateBuilder(args);

>>>>>>> refs/rewritten/onto
// OpenAPI
builder.ConfigureOpenApi();

// RabbitMQ
builder.ConfigureRabbitMq();

<<<<<<< HEAD
// Telemetry
builder.ConfigureOpenTelemetry();
=======
// Observability
builder.AddObservability();
>>>>>>> refs/rewritten/onto

// Cors
builder.ConfigureCors();

// Local Services
builder.AddLocalServices();
<<<<<<< HEAD
builder.Services.AddEndpoints(typeof(Program).Assembly);
=======
>>>>>>> refs/rewritten/onto

var app = builder.Build();

await app.ConfigureExchangesAsync();
app.UseOpenApi();
app.UseCors(CorsPolicies.AllowWebApp);
<<<<<<< HEAD

app.MapEndpoints();
=======
app.MapPublishActionsEndpoints();
>>>>>>> refs/rewritten/onto

app.Run();
