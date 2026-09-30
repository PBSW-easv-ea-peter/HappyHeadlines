using ArticleService.Queue;
using ArticleService.Repositories;
using ArticleService.Resilience;
using ArticleService.Seeding;
using ArticleService.Sharding;
using HappyHeadlines.Observability;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();

builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

// Swagger
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IArticleShardResolver, ArticleShardResolver>();
builder.Services.AddScoped<IArticleReadRepository, ArticleReadRepository>();
builder.Services.AddScoped<IArticleWriteRepository, ArticleWriteRepository>();

builder.AddShardResilience();

// if (!builder.Environment.IsDevelopment())
// {
    builder.Services.AddHostedService<ArticleSeeder>();
// }

var rabbitMq = builder.Configuration.GetSection("RabbitMQ");
builder.Services.AddSingleton(new ConnectionFactory
{
    HostName = rabbitMq["HostName"] ?? throw new InvalidOperationException("RabbitMQ:HostName is not configured."),
    UserName = rabbitMq["UserName"] ?? throw new InvalidOperationException("RabbitMQ:UserName is not configured."),
    Password = rabbitMq["Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is not configured.")
});

builder.Services.AddHostedService<ArticleQueueConsumer>();

var webAppBaseUrl = builder.Configuration["WebApp:BaseUrl"]
    ?? throw new InvalidOperationException("WebApp:BaseUrl is not configured.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        policy
            .WithOrigins(webAppBaseUrl)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowWebApp");

app.MapControllers();

app.Run();
