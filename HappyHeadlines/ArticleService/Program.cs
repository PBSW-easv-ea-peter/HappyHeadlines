using ArticleService.Queue;
using ArticleService.Repositories;
using ArticleService.Seeding;
using ArticleService.Sharding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

// Swagger
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IArticleShardResolver, ArticleShardResolver>();
builder.Services.AddScoped<IArticleReadRepository, ArticleReadRepository>();
builder.Services.AddScoped<IArticleWriteRepository, ArticleWriteRepository>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<ArticleSeeder>();
}

builder.Services.AddHostedService<ArticleQueueConsumer>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy
            .WithOrigins()
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

app.UseCors("Development");

app.MapControllers();

app.Run();
