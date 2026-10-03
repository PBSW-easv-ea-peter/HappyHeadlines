using ArticleService.Cache;
using ArticleService.Queue;
using ArticleService.Repositories;
using ArticleService.Seeding;
using ArticleService.Setup;
using ArticleService.Sharding;

var builder = WebApplication.CreateBuilder(args);

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
