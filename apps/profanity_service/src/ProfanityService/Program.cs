using HappyHeadlines.Observability;
using ProfanityService.Checking;
using ProfanityService.Repositories;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI
builder.Services.AddOpenApi();

// Swagger
builder.Services.AddSwaggerGen();

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
    builder.Configuration["POSTGRES_DB_PWD"] = "Postgres1234";
}

builder.AddObservability();


builder.Services.AddControllers();
builder.Services.AddScoped<IProfanityRepository, ProfanityRepository>();
builder.Services.AddScoped<IProfanityChecker, ProfanityChecker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
