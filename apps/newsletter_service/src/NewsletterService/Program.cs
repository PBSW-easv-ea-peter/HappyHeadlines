using HappyHeadlines.Observability;
using NewsletterService.Setup;

var builder = WebApplication.CreateBuilder(args);

// Email
builder.Services.AddEmailServices();

// Observability from shared/HappyHeadlines.Observability
builder.AddObservability();

// Messaging
builder.ConfigureRabbitMq();

// BG Services
builder.Services.AddNewsletterServices(builder.Configuration);

var app = builder.Build();

await app.ConfigureExchangesAsync();

app.Run();
