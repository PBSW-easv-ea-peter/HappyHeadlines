using ArticleService.Queue;
using ArticleService.Repositories;
using ArticleService.Sharding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IArticleShardResolver, ArticleShardResolver>();
builder.Services.AddScoped<IArticleReadRepository, ArticleReadRepository>();
builder.Services.AddScoped<IArticleWriteRepository, ArticleWriteRepository>();
builder.Services.AddHostedService<ArticleQueueConsumer>();

var app = builder.Build();

app.MapControllers();

app.Run();
