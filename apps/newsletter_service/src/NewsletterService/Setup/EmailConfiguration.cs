using NewsletterService.DeliverNewsletters.EmailService;

namespace NewsletterService.Setup;

public static class EmailConfiguration
{
    public static void AddEmailServices(this IServiceCollection services)
    {
        services.AddSingleton<IEmailSink, EmailSink>();
        services.AddScoped<IEmailService, FakeEmailService>();
    }
}