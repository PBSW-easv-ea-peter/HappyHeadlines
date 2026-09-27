using ProfanityService.Checking;
using ProfanityService.Repositories;
using ProfanityService.Setup;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI
builder.Services.AddOpenApi();

// Swagger
builder.Services.AddSwaggerGen();

// OpenTelemetry
builder.ConfigureOpenTelemetry();

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
