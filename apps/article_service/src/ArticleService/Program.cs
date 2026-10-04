using ArticleService.Cache;
using ArticleService.Queue;
using ArticleService.Repositories;
using ArticleService.Seeding;
using ArticleService.Setup;
using ArticleService.Sharding;
using HappyHeadlines.Observability;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();

builder.Services.AddControllers();

builder.ConfigureOpenApi();
builder.ConfigureCache();

builder.Services.AddSingleton<IArticleShardResolver, ArticleShardResolver>();
builder.Services.AddScoped<IArticleReadRepository, ArticleReadRepository>();
builder.Services.AddScoped<IArticleWriteRepository, ArticleWriteRepository>();

if(builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<ArticleSeeder>();
}

var rabbitMq = builder.Configuration.GetSection("RabbitMQ");
builder.Services.AddSingleton(new ConnectionFactory
{
    HostName = rabbitMq["HostName"] ?? throw new InvalidOperationException("RabbitMQ:HostName is not configured."),
    UserName = rabbitMq["UserName"] ?? throw new InvalidOperationException("RabbitMQ:UserName is not configured."),
    Password = rabbitMq["Password"] ?? throw new InvalidOperationException("RabbitMQ:Password is not configured.")
});

builder.Services.AddHostedService<ArticleQueueConsumer>();
builder.Services.AddHostedService<CacheRefreshService>();


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

app.UseOpenApi();

app.UseCors("AllowWebApp");

app.MapControllers();

app.Run();
