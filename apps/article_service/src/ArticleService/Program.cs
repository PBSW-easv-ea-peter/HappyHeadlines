using ArticleService.Cache;
using ArticleService.Queue;
using ArticleService.Repositories;
using ArticleService.Seeding;
using ArticleService.Setup;
using ArticleService.Sharding;
using HappyHeadlines.Observability;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Set password for Article Database
if (builder.Environment.IsProduction())
{
    if (string.IsNullOrWhiteSpace(
            builder.Configuration["POSTGRES_DB_PWD"]))
    {
        throw new InvalidOperationException(
            "POSTGRES_DB_PWD must be configured in Production.");
    }
}
else
{
    Environment.SetEnvironmentVariable(
        "POSTGRES_DB_PWD",
        "Postgres1234");
}

// Database access
builder.ConfigureLocations();
builder.Services.AddSingleton<IArticleShardResolver, ArticleShardResolver>();
builder.Services.AddScoped<IArticleReadRepository, ArticleReadRepository>();
builder.Services.AddScoped<IArticleWriteRepository, ArticleWriteRepository>();

// Monitoring
builder.AddObservability();

// Networking
builder.Services.AddControllers();

// Cache
builder.ConfigureCache();


// Circuit breaker per shard, used by ArticleQueueConsumer.
builder.AddShardResilience();

if(builder.Environment.IsDevelopment())
{
    builder.ConfigureOpenApi();
    builder.Services.AddHostedService<ArticleSeeder>();
}

// Message broker
var rabbitMq = builder.Configuration.GetSection("RabbitMQ");
builder.Services.AddSingleton(new ConnectionFactory
{
    HostName = rabbitMq["HostName"] ?? throw new InvalidOperationException("RabbitMQ:HostName is not configured."),
    UserName = rabbitMq["UserName"] ?? throw new InvalidOperationException("RabbitMQ:UserName is not configured."),
    Password = rabbitMq["Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is not configured.")
});

builder.Services.AddHostedService<ArticleQueueConsumer>();

// CORS policies
var webAppBaseUrl = builder.Configuration["JournalistWeb:BaseUrl"]
    ?? throw new InvalidOperationException("JournalistWeb:BaseUrl is not configured.");
var readerWebBaseUrl = builder.Configuration["ReaderWeb:BaseUrl"]
    ?? throw new InvalidOperationException("ReaderWeb:BaseUrl is not configured.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        policy
            .WithOrigins(webAppBaseUrl, readerWebBaseUrl)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });

    // For dev
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
}

string policy = app.Environment.IsDevelopment() 
    ? "AllowAll" 
    : "AllowWebApp";
app.UseCors(policy);

app.MapControllers();

app.Run();
