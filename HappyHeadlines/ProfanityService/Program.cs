using ProfanityService.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IProfanityRepository, ProfanityRepository>();

var app = builder.Build();

app.MapControllers();

app.Run();
