using ProfanityService.Checking;
using ProfanityService.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IProfanityRepository, ProfanityRepository>();
builder.Services.AddScoped<IProfanityChecker, ProfanityChecker>();

var app = builder.Build();

app.MapControllers();

app.Run();
