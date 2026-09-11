using CommentService.Profanity;
using CommentService.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();

// Direct HTTP call to ProfanityService - no gateway or UI in between, per this week's
// requirement. The timeout keeps a hanging ProfanityService from blocking CommentService's
// own swim lane; the retry/circuit-breaker policies live in ProfanityClient itself.
builder.Services.AddHttpClient<IProfanityClient, ProfanityClient>(client =>
{
    var baseUrl = builder.Configuration["ProfanityService:BaseUrl"]
        ?? throw new InvalidOperationException("ProfanityService:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(2);
});

var app = builder.Build();

app.MapControllers();

app.Run();
