using NewsletterService.NewsletterDistributors.EmailService;
using NewsletterService.Services.ArticleService;
using NewsletterService.Services.SubscriberService;

namespace NewsletterService.Setup;

public static class ServiceConfigurations
{
    extension(IServiceCollection services)
    {
        public void AddEmailServices()
        {
            services.AddSingleton<IEmailSink, EmailSink>();
            services.AddScoped<IEmailService, FakeEmailService>();
            services.AddScoped<IEmailFactory, EmailFactory>();
        }

        public void AddSubscriberServices()
        {
            services.AddScoped<ISubscriberService, InMemorySubscriberService>();
        }
        
        public void AddArticleServices()
        {
            services.AddScoped<IArticleService, FakeArticleService>();
        }
    }
}